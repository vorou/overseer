﻿using System;
using System.Linq;
using Nest;
using Ploeh.AutoFixture;
using Shouldly;
using Xunit;

namespace Overseer.Tests
{
    public class TenderRepositoryTests
    {
        private static readonly string index = "overseer-test";
        private readonly IFixture fixture = new Fixture();

        public TenderRepositoryTests()
        {
            CreateSut().Clear();
        }

        [Fact]
        public void Save_Always_SavesSource()
        {
            var source = new Tender {Id = "panda", Type = "bear"};
            var sut = CreateSut();

            Save(sut, source);

            var actual = sut.GetById("panda");
            actual.Id.ShouldBe(source.Id);
            actual.Type.ShouldBe(source.Type);
        }

        [Fact]
        public void Clear_Always_RemovesEverything()
        {
            var source = new Tender {Id = "panda", Type = "bear"};
            var sut = CreateSut();
            sut.Save(source);

            sut.Clear();

            sut.GetById("panda").ShouldBe(null);
        }

        [Fact]
        public void GetMostExpensive_NoTenders_ReturnsEmpty()
        {
            var sut = CreateSut();

            var actual = sut.GetMostExpensive();

            actual.ShouldBeEmpty();
        }

        [Fact]
        public void GetMostExpensive_LimitIsMoreThanTotal_ReturnsAll()
        {
            var sut = CreateSut();
            Save(sut, CreateActiveTender());

            var actual = sut.GetMostExpensive(1);

            actual.Count().ShouldBe(1);
        }

        [Fact]
        public void GetMostExpensive_Always_ReadsTenderProperly()
        {
            var tender = CreateActiveTender();
            var sut = CreateSut();
            Save(sut, tender);

            var actual = sut.GetMostExpensive(1);

            actual.Single().ShouldBe(tender.Ish());
        }

        [Fact]
        public void GetMostExpensive_TwoTenders_ReturnsMoreExpensiveOne()
        {
            var sut = CreateSut();
            var expensive = CreateActiveTender();
            expensive.TotalPrice = 1000M;
            var cheap = CreateActiveTender();
            cheap.TotalPrice = 1M;
            Save(sut, expensive);
            Save(sut, cheap);

            var actual = sut.GetMostExpensive(1);

            actual.Single().ShouldBe(expensive.Ish());
        }

        [Fact]
        public void GetMostExpensive_TenderWasPublishedYesterday_ReturnsEmpty()
        {
            var sut = CreateSut();
            var yesterdayTender = fixture.Create<Tender>();
            yesterdayTender.PublishDate = DateTime.Now.AddDays(-1);
            Save(sut, yesterdayTender);

            var actual = sut.GetMostExpensive();

            actual.Count().ShouldBe(0);
        }

        private static TenderRepository CreateSut()
        {
            return new TenderRepository(index);
        }

        private static void Save(TenderRepository sut, Tender tender)
        {
            sut.Save(tender);
            new ElasticClient(new ConnectionSettings(new Uri("http://localhost:9200")).SetDefaultIndex(index)).Refresh<Tender>();
        }

        private Tender CreateActiveTender()
        {
            var tender = fixture.Create<Tender>();
            tender.PublishDate = DateTime.Now;
            return tender;
        }
    }
}