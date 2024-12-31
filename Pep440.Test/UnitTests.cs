using FluentAssertions;
namespace Pep440.Test {
	public class UnitTests {
		[Fact]
		public void Test1() {
			var result = true;
			result.Should().Be(true);
		}
	}
}
