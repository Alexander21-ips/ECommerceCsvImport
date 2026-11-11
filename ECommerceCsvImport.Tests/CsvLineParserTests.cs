using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommerceCsvImport.Services;
using Xunit;

namespace ECommerceCsvImport.Tests
{
    public class CsvLineParserTests
    {
        [Fact]
        public void Parse_SimpleLine_ReturnsCorrectFields()
        {
            var line = "2001,Книга,Описание,1500";
            var result = CsvLineParser.Parse(line);

            Assert.Equal(4, result.Count);
            Assert.Equal("2001", result[0]);
            Assert.Equal("1500", result[3]);
        }

        [Fact]
        public void Parse_EmptyFields_ReturnsEmptyStrings()
        {
            var line = "2001,,Описание,,";
            var result = CsvLineParser.Parse(line);

            Assert.Equal(5, result.Count);
            Assert.Equal(string.Empty, result[1]);
            Assert.Equal(string.Empty, result[4]);
        }

        [Fact]
        public void Parse_CommaInsideQuotes_DoesNotSplitField()
        {
            var line = "2001,\"Книга, часть 1\",Описание,1500";
            var result = CsvLineParser.Parse(line);

            Assert.Equal(4, result.Count);
            Assert.Equal("Книга, часть 1", result[1]);
        }
    }
}