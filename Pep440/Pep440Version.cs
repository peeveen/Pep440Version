using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Pep440 {
	/// <summary>
	/// Prerelease type
	/// </summary>
	public enum PrereleaseType {
		/// <summary>
		/// Alpha release
		/// </summary>
		Alpha,
		/// <summary>
		/// Beta release
		/// </summary>
		Beta,
		/// <summary>
		/// Release candidate
		/// </summary>
		ReleaseCandidate
	}

	/// <summary>
	/// Prerelease information.
	/// </summary>
	public class Pep440PrereleaseSegment {
		/// <summary>
		/// The prerelease type.
		/// </summary>
		public PrereleaseType PrereleaseType { get; }
		/// <summary>
		/// The prerelease number
		/// </summary>
		public int PrereleaseNumber { get; }
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="prereleaseType"></param>
		/// <param name="prereleaseNumber"></param>
		public Pep440PrereleaseSegment(PrereleaseType prereleaseType, int prereleaseNumber) {
			PrereleaseType = prereleaseType;
			PrereleaseNumber = prereleaseNumber;
		}
	}

	/// <summary>
	/// A representation of a version number that follows the PEP440 specification.
	/// https://www.python.org/dev/peps/pep-0440/
	/// </summary>
	public class Pep440Version {
		private const string PEP440_VERSION_PATTERN = @"^(?:(?<epoch>\d+)!)?(?<release>\d+(\.\d+)*)(?<pre>(a|b|c|rc)\d+)?(?<post>\.post\d+)?(?<dev>\.dev\d+)?(?<local>\+[a-zA-Z0-9]+)?$";

		/// <summary>
		/// Version number parts. There will ALWAYS be at least one in this list.
		/// </summary>
		public IReadOnlyList<int> ReleaseVersionNumbers { get; }
		/// <summary>
		/// The post-release version, or null if there is not one.
		/// </summary>
		public int? PostReleaseNumber { get; }
		/// <summary>
		/// The dev-release version, or null if there is not one.
		/// </summary>
		public int? DevelopmentReleaseNumber { get; }
		/// <summary>
		/// The epoch number. Zero if not specified.
		/// </summary>
		public int EpochNumber { get; }
		/// <summary>
		/// Local version label. Null if not specified.
		/// </summary>
		public string LocalVersionLabel { get; }
		/// <summary>
		/// Prerelease information. Null if not a prerelease.
		/// </summary>
		public Pep440PrereleaseSegment PrereleaseSegment { get; }

		/// <summary>
		/// Constructor for public versions.
		/// </summary>
		public Pep440Version(
			IEnumerable<int> publicReleaseVersionNumbers,
			Pep440PrereleaseSegment prereleaseSegment = null,
			int? postReleaseNumber = null,
			int? developmentReleaseNumber = null,
			int epochNumber = 0,
			string localVersionLabel = null
		) {
			if (!publicReleaseVersionNumbers.Any())
				throw new ArgumentException("Version must have at least one public release number", nameof(publicReleaseVersionNumbers));
			ReleaseVersionNumbers = publicReleaseVersionNumbers.ToList();
			PrereleaseSegment = prereleaseSegment;
			PostReleaseNumber = postReleaseNumber;
			DevelopmentReleaseNumber = developmentReleaseNumber;
			EpochNumber = epochNumber;
			LocalVersionLabel = localVersionLabel;
		}

		private static PrereleaseType GetPrereleaseType(string value) {
			if (value == "a") return PrereleaseType.Alpha;
			if (value == "b") return PrereleaseType.Beta;
			// Regex parsing will ensure that the only possible remaining values are c or rc, which both mean ...
			return PrereleaseType.ReleaseCandidate;
		}

		private static string GetPrereleaseString(PrereleaseType prereleaseType) {
			if (prereleaseType == PrereleaseType.Alpha) return "a";
			if (prereleaseType == PrereleaseType.Beta) return "b";
			return "rc";
		}

		/// <inheritdoc/>
		public override string ToString() {
			string version = string.Join(".", ReleaseVersionNumbers);
			if (EpochNumber > 0) version = $"{EpochNumber}!{version}";
			if (PrereleaseSegment != null) version = $"{version}{GetPrereleaseString(PrereleaseSegment.PrereleaseType)}{PrereleaseSegment.PrereleaseNumber}";
			if (PostReleaseNumber.HasValue) version = $"{version}.post{PostReleaseNumber}";
			if (DevelopmentReleaseNumber.HasValue) version = $"{version}.dev{DevelopmentReleaseNumber}";
			if (!string.IsNullOrEmpty(LocalVersionLabel)) version = $"{version}+{LocalVersionLabel}";
			return version;
		}

		/// <summary>
		/// Parses a string to a Pep440Version, if possible.
		/// </summary>
		/// <param name="version">String to parse.</param>
		/// <returns>A parsed version object.</returns>
		public static Pep440Version Parse(string version) {
			var match = Regex.Match(version, PEP440_VERSION_PATTERN);
			if (!match.Success)
				throw new ArgumentException("Invalid PEP440 version string", nameof(version));

			int epoch = match.Groups["epoch"].Success ? int.Parse(match.Groups["epoch"].Value) : 0;
			var release = Array.ConvertAll(match.Groups["release"].Value.Split('.'), int.Parse);
			var prerelease = match.Groups["pre"].Success ? new Pep440PrereleaseSegment(
				GetPrereleaseType(string.Join(string.Empty, match.Groups["pre"].Value.Where(c => !char.IsDigit(c)))),
				int.Parse(string.Join(string.Empty, match.Groups["pre"].Value.Where(char.IsDigit)))
			) : null;
			int? post = match.Groups["post"].Success ? int.Parse(match.Groups["post"].Value.Substring(5)) : (int?)null;
			int? dev = match.Groups["dev"].Success ? int.Parse(match.Groups["dev"].Value.Substring(4)) : (int?)null;
			string local = match.Groups["local"].Success ? match.Groups["local"].Value.Substring(1) : null;

			return new Pep440Version(release, prerelease, post, dev, epoch, local);
		}

		/// <summary>
		/// Attempts to parse a string to a Pep440Version.
		/// </summary>
		/// <param name="version">Version string.</param>
		/// <param name="pep440Version">PEP440 version if parsed successfully.</param>
		/// <returns>True if parse was successful.</returns>
		public static bool TryParse(string version, out Pep440Version pep440Version) {
			try {
				pep440Version = Parse(version);
				return true;
			} catch {
				pep440Version = null;
				return false;
			}
		}

		/// <summary>
		/// Compares this version to another.
		/// </summary>
		/// <param name="version">Version to compare against.</param>
		/// <returns>A value greater than zero if this object is a later version.
		/// A value less than zero if this object is an earlier version.
		/// A value equal to zero if the objects refer to the same version.
		/// Dev versions are considered earlier than non-dev versions.
		/// If all else is equal, the local version label is compared alphabetically.
		/// </returns>
		public int CompareTo(Pep440Version version) {
			int CompareVersionNumbers() {
				var vdiff = 0;
				for (var i = 0; i < ReleaseVersionNumbers.Count; i++) {
					if (i >= version.ReleaseVersionNumbers.Count) {
						vdiff = 1;
						break;
					}
					vdiff = ReleaseVersionNumbers[i] - version.ReleaseVersionNumbers[i];
					if (vdiff != 0) break;
				}
				if (vdiff == 0 && ReleaseVersionNumbers.Count < version.ReleaseVersionNumbers.Count)
					vdiff = -1;
				return vdiff;
			}

			var diff = EpochNumber - version.EpochNumber;
			if (diff == 0) {
				diff = CompareVersionNumbers();
				if (diff == 0) {
					if (PrereleaseSegment != null && version.PrereleaseSegment == null)
						diff = -1;
					else if (PrereleaseSegment == null && version.PrereleaseSegment != null)
						diff = 1;
					else if (PrereleaseSegment != null && version.PrereleaseSegment != null) {
						diff = (int)PrereleaseSegment.PrereleaseType - (int)version.PrereleaseSegment.PrereleaseType;
						if (diff == 0)
							diff = PrereleaseSegment.PrereleaseNumber - version.PrereleaseSegment.PrereleaseNumber;
					}
					if (diff == 0) {
						// A post-release version obviously counts as a later version than a non-post-release version.
						diff = PostReleaseNumber.GetValueOrDefault() - version.PostReleaseNumber.GetValueOrDefault();
						if (diff == 0) {
							// BUT! A dev version counts as an EARLIER version than a non-dev version.
							diff = version.DevelopmentReleaseNumber.GetValueOrDefault() - DevelopmentReleaseNumber.GetValueOrDefault();
							if (diff == 0)
								// We're down to brass tacks ... compare the local version label.
								diff = string.Compare(LocalVersionLabel, version.LocalVersionLabel, StringComparison.Ordinal);
						}
					}
				}
			}
			return diff;
		}
	}
}

