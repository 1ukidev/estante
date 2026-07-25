using System;
using NUnit.Framework;

namespace Estante.Game.Tests
{
    [TestFixture]
    public class GoogleSearchTest
    {
        [Test]
        public void TestCreatesEscapedSearchUrl()
        {
            string url = GoogleSearch.CreateUrl("ação & pesquisa");

            Assert.That(url, Is.EqualTo("https://www.google.com/search?q=a%C3%A7%C3%A3o%20%26%20pesquisa"));
        }

        [Test]
        public void TestRejectsEmptyQuery()
        {
            Assert.Throws<ArgumentException>(() => GoogleSearch.CreateUrl("  "));
        }
    }
}
