using System;
using FluentAssertions;

namespace Pep440.Test {
	public class Pep440Tests {
		[Theory]
		[MemberData(nameof(ValidParsingTestData))]
		public void TestValidPep440VersionStringParsing(string versionString, Pep440Version expectedVersion) {
			var parsed = Pep440Version.Parse(versionString);
			parsed.Should().BeEquivalentTo(expectedVersion);
			parsed.ToString().Should().BeEquivalentTo(versionString);
		}

		[Theory]
		[MemberData(nameof(InvalidVersionStrings))]
		public void TestInvalidPep440VersionStringParsing(string versionString) =>
			new Action(() => Pep440Version.Parse(versionString)).Should().Throw<ArgumentException>();

		public static TheoryData<string, Pep440Version> ValidParsingTestData => new() {
			{ "1", new Pep440Version([1]) },
			{ "1.2", new Pep440Version([1,2]) },
			{ "1.2.66.98756.221121.54", new Pep440Version([1,2,66,98756,221121,54]) },
			{ "55!1.2", new Pep440Version([1,2], epochNumber:55) },
			{ "1.7.3.4a99", new Pep440Version([1,7,3,4], prereleaseSegment: new Pep440PrereleaseSegment(PrereleaseType.Alpha, 99)) },
			{ "1000.34234.546456.231231rc993939393", new Pep440Version([1000,34234,546456,231231], prereleaseSegment: new Pep440PrereleaseSegment(PrereleaseType.ReleaseCandidate, 993939393)) },
			{ "2!5b3.post66.dev983", new Pep440Version([5], prereleaseSegment: new Pep440PrereleaseSegment(PrereleaseType.Beta, 3), postReleaseNumber: 66, developmentReleaseNumber:983, epochNumber: 2) },
			{ "2!5b3.post66.dev983+blah", new Pep440Version([5], prereleaseSegment: new Pep440PrereleaseSegment(PrereleaseType.Beta, 3), postReleaseNumber: 66, developmentReleaseNumber:983, epochNumber: 2, localVersionLabel: "blah") },
		};

		public static TheoryData<string> InvalidVersionStrings => [
			string.Empty,
			"hello",
			"6,9",
			"a!2.4.5.6",
			"1.3.4alpha4",
			"1.3.4.posst4",
		];
	}
}
