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
    public class RepositoryTests
    {
        [Fact]
        public void Add_AddsProduct_GetAllContainsIt()
        {
            var repository = new InMemoryProductRepository();
            var product = new Product { Sku = "2001", Name = "Книга", Price = 1500m };

            repository.Add(product);

            var all = repository.GetAll();
            Assert.Single(all);
            Assert.Equal("2001", all[0].Sku);
        }

        [Fact]
        public void GetBySku_IgnoresCaseAndWhitespace()
        {
            var repository = new InMemoryProductRepository();
            repository.Add(new Product { Sku = "2001", Name = "Книга" });

            var result = repository.GetBySku(" 2001 ");

            Assert.NotNull(result);
        }

        [Fact]
        public void EmptyRepository_ReturnsZeroAndNull()
        {
            var repository = new InMemoryProductRepository();
            Assert.Equal(0, repository.Count);
            Assert.Empty(repository.GetAll());
            Assert.Null(repository.GetBySku("2001"));
        }
    }
}