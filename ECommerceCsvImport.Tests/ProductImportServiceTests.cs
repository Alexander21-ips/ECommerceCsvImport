using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Xunit;

namespace ECommerceCsvImport.Tests
{
    public class ProductImportServiceTests
    {
        private const string Header = "sku,name,description,price,currency,category_name,stock,image_url,is_active,isbn,author,publisher,publication_year";

        [Fact]
        public void Import_Successful_OneRow()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[]
            {
                Header,
                "2001,C# для начинающих,Учебник,1500,RUB,Программирование,10,/images/csharp.jpg,true,978-5-17-123456-7,Иванов И.И.,Эксмо,2022"
            };

            var result = service.ImportFromLines(lines);

            Assert.Equal(1, result.TotalRows);
            Assert.Equal(1, result.ImportedCount);
            Assert.Empty(result.Errors);

            var product = repo.GetBySku("2001");
            Assert.NotNull(product);
            Assert.Equal("Иванов И.И.", product.Author);
        }

        [Fact]
        public void Import_EmptyFile_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new string[0];

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
        }

        [Fact]
        public void Import_InvalidPrice_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[]
            {
                Header,
                "2001,C# для начинающих,Учебник,-500,RUB,Программирование,10,/images/csharp.jpg,true,978-5-17-123456-7,Иванов И.И.,Эксмо,2022"
            };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Contains(result.Errors, e => e.Message.Contains("Поле price должно быть числом больше 0"));
        }

        [Fact]
        public void Import_DuplicateSku_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[]
            {
                Header,
                "2001,C# для начинающих,Учебник,1500,RUB,Программирование,10,/images/csharp.jpg,true,978-5-17-123456-7,Иванов И.И.,Эксмо,2022",
                "2001,Другая книга,Другое,1800,RUB,Программирование,5,/images/other.jpg,true,978-5-17-111111-1,Петров П.П.,Питер,2023"
            };

            var result = service.ImportFromLines(lines);

            Assert.Equal(2, result.TotalRows);
            Assert.Equal(1, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Артикул товара уже существует", result.Errors[0].Message);
        }

        [Fact]
        public void Import_InvalidIsbn_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[]
            {
                Header,
                "2001,C# для начинающих,Учебник,1500,RUB,Программирование,10,/images/csharp.jpg,true,12345,Иванов И.И.,Эксмо,2022"
            };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Contains(result.Errors, e => e.Message.Contains("Поле isbn обязательно и должно содержать 10 или 13 символов"));
        }

        [Fact]
        public void Import_InvalidYear_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[]
            {
                Header,
                "2001,C# для начинающих,Учебник,1500,RUB,Программирование,10,/images/csharp.jpg,true,978-5-17-123456-7,Иванов И.И.,Эксмо,1800"
            };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Contains(result.Errors, e => e.Message.Contains("Год издания должен быть целым числом"));
        }

        [Fact]
        public void Import_MissingAuthor_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[]
            {
                Header,
                "2001,C# для начинающих,Учебник,1500,RUB,Программирование,10,/images/csharp.jpg,true,978-5-17-123456-7,,Эксмо,2022"
            };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Contains(result.Errors, e => e.Message.Contains("Поле author обязательно"));
        }
    }
}