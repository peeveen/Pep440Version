﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Pep440 {

	/// <summary>
	///
	/// </summary>
	public enum PrereleaseType {
		/// <summary>
		///
		/// </summary>
		Alpha,
		/// <summary>
		///
		/// </summary>
		Beta,
		/// <summary>
		///
		/// </summary>
		ReleaseCandidate
	}

	/// <summary>
	///
	/// </summary>
	public class Pep440PrereleaseSegment {
		/// <summary>
		///
		/// </summary>
		public PrereleaseType PrereleaseType { get; }
		/// <summary>
		///
		/// </summary>
		public int PrereleaseNumber { get; }
		/// <summary>
		///
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
		public Pep440Version(int[] publicReleaseVersionNumbers, Pep440PrereleaseSegment prereleaseSegment = null, int? postReleaseNumber = null, int? developmentReleaseNumber = null, int epochNumber = 0, string localVersionLabel = null) {
			ReleaseVersionNumbers = publicReleaseVersionNumbers;
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
	}
}
