using System;
using FluentAssertions;

namespace Pep440.Test {
	public class Pep440Tests {
		[Theory]
		[MemberData(nameof(ValidParsingTestData))]
		public void TestValidPep440VersionStringParsing(string versionString, Pep440Version expectedVersion, string? expectedStringConversionResult) {
			var parsed = Pep440Version.Parse(versionString);
			parsed.Should().BeEquivalentTo(expectedVersion);
			parsed.ToString().Should().BeEquivalentTo(expectedStringConversionResult ?? versionString);
		}

		[Theory]
		[MemberData(nameof(InvalidVersionStrings))]
		public void TestInvalidPep440VersionStringParsing(string versionString) =>
			new Action(() => Pep440Version.Parse(versionString)).Should().Throw<ArgumentException>();

		[Theory]
		[MemberData(nameof(ComparisonTestData))]
		public void TestVersionComparisons(string versionString1, string versionString2, int expectedResult) {
			var parsed1 = Pep440Version.Parse(versionString1);
			var parsed2 = Pep440Version.Parse(versionString2);
			var result = parsed1.CompareTo(parsed2);
			var inverseResult = parsed2.CompareTo(parsed1);

			if (expectedResult < 0) {
				result.Should().BeLessThan(0);
				inverseResult.Should().BeGreaterThan(0);
			} else if (expectedResult > 0) {
				result.Should().BeGreaterThan(0);
				inverseResult.Should().BeLessThan(0);
			} else {
				result.Should().Be(0);
				inverseResult.Should().Be(0);
			}

			var anotherParsed1 = Pep440Version.Parse(versionString1);
			var anotherParsed2 = Pep440Version.Parse(versionString2);
			var expectedSameResult = parsed1.CompareTo(anotherParsed1);
			expectedSameResult.Should().Be(0);
			expectedSameResult = parsed2.CompareTo(anotherParsed2);
			expectedSameResult.Should().Be(0);
		}

		public static TheoryData<string, Pep440Version, string?> ValidParsingTestData => new() {
			{ "1", new Pep440Version([1]), null },
			{ "1.2", new Pep440Version([1,2]), null },
			{ "1.2.66.98756.221121.54", new Pep440Version([1,2,66,98756,221121,54]), null },
			{ "55!1.2", new Pep440Version([1,2], epochNumber:55), null },
			{ "1.7.3.4a99", new Pep440Version([1,7,3,4], prereleaseSegment: new Pep440PrereleaseSegment(PrereleaseType.Alpha, 99)), null },
			{ "1000.34234.546456.231231rc993939393", new Pep440Version([1000,34234,546456,231231], prereleaseSegment: new Pep440PrereleaseSegment(PrereleaseType.ReleaseCandidate, 993939393)), null },
			{ "2!5b3.post66.dev983", new Pep440Version([5], prereleaseSegment: new Pep440PrereleaseSegment(PrereleaseType.Beta, 3), postReleaseNumber: 66, developmentReleaseNumber:983, epochNumber: 2), null },
			{ "2!5b3.post66.dev983+blah", new Pep440Version([5], prereleaseSegment: new Pep440PrereleaseSegment(PrereleaseType.Beta, 3), postReleaseNumber: 66, developmentReleaseNumber:983, epochNumber: 2, localVersionLabels: ["blah"]), null },
			{ "1", new Pep440Version([1]), null },
			{ "v1.2", new Pep440Version([1,2]), "1.2" },
			{ "1.2.66.98756.221121.54", new Pep440Version([1,2,66,98756,221121,54]), null },
			{ "v55!1.2", new Pep440Version([1,2], epochNumber:55), "55!1.2" },
			{ "1.7.3.4-alpha_99", new Pep440Version([1,7,3,4], prereleaseSegment: new Pep440PrereleaseSegment(PrereleaseType.Alpha, 99)), "1.7.3.4a99" },
			{ "1000.34234.546456.231231pre.993939393", new Pep440Version([1000,34234,546456,231231], prereleaseSegment: new Pep440PrereleaseSegment(PrereleaseType.ReleaseCandidate, 993939393)), "1000.34234.546456.231231rc993939393" },
			{ "2!5b3post66.dev983", new Pep440Version([5], prereleaseSegment: new Pep440PrereleaseSegment(PrereleaseType.Beta, 3), postReleaseNumber: 66, developmentReleaseNumber:983, epochNumber: 2), "2!5b3.post66.dev983" },
			{ "2!5b3post66-dev983+blah_yap-foo.bar", new Pep440Version([5], prereleaseSegment: new Pep440PrereleaseSegment(PrereleaseType.Beta, 3), postReleaseNumber: 66, developmentReleaseNumber:983, epochNumber: 2, localVersionLabels: ["blah","yap","foo", "bar"]), "2!5b3.post66.dev983+blah.yap.foo.bar" },
		};

		public static TheoryData<string> InvalidVersionStrings => [
			string.Empty,
			"hello",
			"6,9",
			"a!2.4.5.6",
			"1.3.4alphha4",
			"1.3.4.posst4",
		];

		public static TheoryData<string, string, int> ComparisonTestData => new() {
			{ "1", "2", -1 },
			{ "1!1", "1", 1 },
			{ "1.2.3.4.5", "1.2.3.4.6", -1 },
			{ "1.2.3.4.5", "1.2.3.4", 1 },
			{ "1.2.3.4.0", "1.2.3.4", 1 },
			{ "1.2.dev1", "1.2", -1 },
			{ "1.2.post1", "1.2", 1 },
			{ "1.2.post1.dev1", "1.2.post1", -1 },
			{ "1.2a3", "1.2b3", -1 },
			{ "1.2b3", "1.2c3", -1 },
			{ "1.2b3", "1.2rc3", -1 },
			{ "1.2c3", "1.2rc4", -1 },
			{ "1.2rc3", "1.2rc4", -1 },
			{ "1.2+flam.5", "1.2+flam.6", -1 },
			{ "1.2+flam.five", "1.2+flam.0", -1 },
			{ "1.2+flam.five", "1.2+flam.five.six", -1 },
		};
	}
}
