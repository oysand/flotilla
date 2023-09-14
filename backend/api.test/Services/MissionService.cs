﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Controllers.Models;
using Api.Database.Context;
using Api.Database.Models;
using Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Api.Test.Services
{
    [Collection("Database collection")]
    public class MissionServiceTest : IDisposable
    {
        private readonly FlotillaDbContext _context;
        private readonly ILogger<MissionRunService> _logger;
        private readonly MissionRunService _missionRunService;

        public MissionServiceTest(DatabaseFixture fixture)
        {
            _context = fixture.NewContext;
            _logger = new Mock<ILogger<MissionRunService>>().Object;
            _missionRunService = new MissionRunService(_context, _logger);
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task ReadIdDoesNotExist()
        {
            var missionRun = await _missionRunService.ReadById("some_id_that_does_not_exist");
            Assert.Null(missionRun);
        }

        [Fact]
        public async Task Create()
        {
            var robot = _context.Robots.First();
            int nReportsBefore = _missionRunService
                .ReadAll(new MissionRunQueryStringParameters())
                .Result.Count;

            var testInstallation = new Installation
            {
                InstallationCode = "test",
                Name = "test test"
            };
            var testPlant = new Plant
            {
                PlantCode = "test",
                Name = "test test",
                Installation = testInstallation
            };
            var testDeck = new Deck
            {
                Plant = testPlant,
                Installation = testInstallation,
                Name = "testDeck"
            };
            var testArea = new Area
            {
                Deck = testDeck,
                Installation = testInstallation,
                Plant = testPlant,
                Name = "testArea",
                MapMetadata = new MapMetadata() { MapName = "testMap" },
                DefaultLocalizationPose = new Pose(),
                SafePositions = new List<SafePosition>()
            };

            testDeck.DefaultLocalizationArea = testArea;

            MissionRun missionRun =
                new()
                {
                    Name = "testMission",
                    Robot = robot,
                    MissionId = Guid.NewGuid().ToString(),
                    Map = new MapMetadata() { MapName = "testMap" },
                    Area = testArea,
                    Deck = testDeck,
                    InstallationCode = "testInstallation",
                    DesiredStartTime = DateTime.Now
                };

            await _missionRunService.Create(missionRun);
            int nReportsAfter = _missionRunService
                .ReadAll(new MissionRunQueryStringParameters())
                .Result.Count;

            Assert.Equal(nReportsBefore + 1, nReportsAfter);
        }
    }
}
