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
		private const string PEP440_VERSION_PATTERN = @"^v?(?:(?<epoch>\d+)!)?(?<release>\d+(\.\d+)*)(?<pre>[-_.]?(a|b|c|rc|alpha|beta|pre|preview)([-_.]?\d+)?)?(?<post>[-_.]?(post|rev|r)([-_.]?\d+)?)?(?<dev>[-_.]?dev([-_.]?\d+)?)?(?<local>\+([a-zA-Z0-9]+[-_.]?)+)?$";
		private static readonly char[] PepSeparators = new char[] { '.', '-', '_' };

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
		public IReadOnlyList<string> LocalVersionLabels { get; }
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
			IEnumerable<string> localVersionLabels = null
		) {
			if (!publicReleaseVersionNumbers.Any())
				throw new ArgumentException("Version must have at least one public release number", nameof(publicReleaseVersionNumbers));
			ReleaseVersionNumbers = publicReleaseVersionNumbers.ToList();
			PrereleaseSegment = prereleaseSegment;
			PostReleaseNumber = postReleaseNumber;
			DevelopmentReleaseNumber = developmentReleaseNumber;
			EpochNumber = epochNumber;
			LocalVersionLabels = localVersionLabels?.ToList() ?? new List<string>();
		}

		private static PrereleaseType GetPrereleaseType(string value) {
			if (value == "a" || value == "alpha") return PrereleaseType.Alpha;
			if (value == "b" || value == "beta") return PrereleaseType.Beta;
			// Regex parsing will ensure that the only possible remaining values are ...
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
			if (LocalVersionLabels.Any()) version = $"{version}+{string.Join(".", LocalVersionLabels)}";
			return version;
		}

		/// <summary>
		/// Parses a string to a Pep440Version, if possible.
		/// </summary>
		/// <param name="version">String to parse.</param>
		/// <returns>A parsed version object.</returns>
		public static Pep440Version Parse(string version) {
			IEnumerable<string> ParseLocalVersionLabels(string localVersionLabelString) {
				if (localVersionLabelString == null) return new List<string>();
				return localVersionLabelString.Split(PepSeparators).ToList();
			}
			Pep440PrereleaseSegment GetPrereleaseSegment(Group g) {
				var prereleaseString = g.Value.Strip(PepSeparators);
				return new Pep440PrereleaseSegment(
					GetPrereleaseType(string.Join(string.Empty, prereleaseString.Where(c => !char.IsDigit(c)))),
					int.Parse(string.Join(string.Empty, prereleaseString.Where(char.IsDigit)))
				);
			}
			version = version.Trim().ToLowerInvariant();
			var match = Regex.Match(version, PEP440_VERSION_PATTERN);
			if (!match.Success)
				throw new ArgumentException("Invalid PEP440 version string", nameof(version));

			var epochGroup = match.Groups["epoch"];
			int epoch = epochGroup.Success ? int.Parse(epochGroup.Value) : 0;
			var release = Array.ConvertAll(match.Groups["release"].Value.Split('.'), int.Parse);
			var prereleaseGroup = match.Groups["pre"];
			var prerelease = prereleaseGroup.Success ? GetPrereleaseSegment(prereleaseGroup) : null;
			var postGroup = match.Groups["post"];
			int? post = postGroup.Success ? int.Parse(postGroup.Value.Strip(PepSeparators).Substring(4)) : (int?)null;
			var devGroup = match.Groups["dev"];
			int? dev = devGroup.Success ? int.Parse(devGroup.Value.Strip(PepSeparators).Substring(3)) : (int?)null;
			var localGroup = match.Groups["local"];
			string local = localGroup.Success ? localGroup.Value.Substring(1) : null;

			return new Pep440Version(release, prerelease, post, dev, epoch, ParseLocalVersionLabels(local));
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
		/// If all else is equal, the local version labels are compared alphabetically.
		/// </returns>
		public int CompareTo(Pep440Version version) {
			int CompareCollections<T>(IReadOnlyList<T> c1, IReadOnlyList<T> c2, Func<T, T, int> comparer) {
				var vdiff = 0;
				for (var i = 0; i < c1.Count; i++) {
					if (i >= c2.Count) {
						vdiff = 1;
						break;
					}
					vdiff = comparer(c1[i], c2[i]);
					if (vdiff != 0) break;
				}
				if (vdiff == 0 && c1.Count < c2.Count)
					vdiff = -1;
				return vdiff;
			}
			int CompareVersionNumbers() => CompareCollections(ReleaseVersionNumbers, version.ReleaseVersionNumbers, (a, b) => a - b);
			int CompareLocalVersionLabels(string l1, string l2) {
				// According to the PEP440 spec ...
				// "Comparison and ordering of local versions considers each segment of the local version (divided by a .) separately.
				// If a segment consists entirely of ASCII digits then that section should be considered an integer for comparison
				// purposes and if a segment contains any ASCII letters then that segment is compared lexicographically with case
				// insensitivity. When comparing a numeric and lexicographic segment, the numeric section always compares as greater
				// than the lexicographic segment. Additionally a local version with a great number of segments will always compare
				// as greater than a local version with fewer segments, as long as the shorter local version’s segments match the
				// beginning of the longer local version’s segments exactly."

				int i1 = 0, i2 = 0;
				var parsedInt1 = l1.All(char.IsDigit) && int.TryParse(l1, out i1);
				var parsedInt2 = l2.All(char.IsDigit) && int.TryParse(l2, out i2);
				if (parsedInt1 && parsedInt2)
					return i1 - i2;
				if (parsedInt1)
					return 1;
				if (parsedInt2)
					return -1;
				return string.Compare(l1, l2, StringComparison.Ordinal);
			}
			int CompareLocalVersionLabelCollections() => CompareCollections(LocalVersionLabels, version.LocalVersionLabels, CompareLocalVersionLabels);

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
								// We're down to brass tacks ... compare the local version labels.
								diff = CompareLocalVersionLabelCollections();
						}
					}
				}
			}
			return diff;
		}
	}
}

/// <summary>
/// Extensions for string manipulation.
/// </summary>
internal static class StringExtensions {
	public static string Strip(this string value, char[] chars) {
		return new string(value.Where(c => !chars.Contains(c)).ToArray());
	}
}