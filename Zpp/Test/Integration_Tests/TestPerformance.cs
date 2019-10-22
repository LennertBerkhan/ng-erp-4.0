using System;
using Xunit;
using Zpp.Test.Configuration;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests
{
    public class TestPerformance : AbstractTest
    {
        
        
        public TestPerformance() : base(initDefaultTestConfig: false)
        {

        }
        
        private void InitThisTest(string testConfiguration)
        {
            InitTestScenario(testConfiguration);
        }

    
        [Theory]
        [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        public void TestMaxTimeForMrpRunIsNotExceeded(string testConfigurationFileName)
        {
            const int MAX_TIME_FOR_MRP_RUN = 90;
            
            InitThisTest(testConfigurationFileName);
            
            DateTime startTime = DateTime.UtcNow;

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();

            DateTime endTime = DateTime.UtcNow;
            double neededTime = (endTime - startTime).TotalMilliseconds / 1000;
            Assert.True( neededTime < MAX_TIME_FOR_MRP_RUN,
                $"MrpRun for example use case ({TestConfiguration.Name}) " +
                $"takes longer than {MAX_TIME_FOR_MRP_RUN} seconds: {neededTime}");
        }

        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_LOTSIZE_2)]
        public void TestPerformanceStudy(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartPerformanceStudy();
        }
    }
}