using Microsoft.Azure.Devices.Client.DiagnosticProvider;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.Client.Test
{
    /// <summary>
    /// Summary description for DiagnosticProviderTest
    /// </summary>
    [TestClass]
    public class DiagnosticProviderTest
    {
        [TestMethod]
        public void DoNotSamplingWhenSamplingSourceIsNone()
        {
            var diagnosticProvider = new ContinuousDiagnosticProvider(SamplingRateSource.None);
            Assert.AreEqual(diagnosticProvider.SamplingOn, false);
            for (var i = 0; i < 100; i++)
            {
                diagnosticProvider.Process(new Message());
            }
            Assert.AreEqual(diagnosticProvider.GetSampledCount(), 0);
        }

        [TestMethod]
        public void DoNotSamplingWhenNeedSamplingIsFalse()
        {
            var diagnosticProvider = new ContinuousDiagnosticProvider(SamplingRateSource.None);
            Assert.AreEqual(diagnosticProvider.SamplingOn, false);
            for (var i = 0; i < 100; i++)
            {
                diagnosticProvider.Process(new Message());
            }
            Assert.AreEqual(diagnosticProvider.GetSampledCount(), 0);
        }

        [TestMethod]
        public void SamplingRateIsDefaultZeroWhenUsingServerSamplingSource()
        {
            var diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Server, 50);
            Assert.AreEqual(diagnosticProvider.SamplingRatePercentage, 0);
            Assert.AreNotEqual(diagnosticProvider.SamplingRatePercentage, 50);
        }

        [TestMethod]
        public void SetDefaultValueWhenReceivedInvalidTwin()
        {
            var diagnosticProvider = new ContinuousDiagnosticProvider(SamplingRateSource.Server);

            var twin = new Twin();
            twin.Properties.Desired["diag_enableInvalid"] = true;
            twin.Properties.Desired[BaseDiagnosticProvider.TwinDiagSamplingRateKey] = 10;


            diagnosticProvider.SetSamplingConfigFromTwin(twin.Properties.Desired);
            Assert.AreEqual(diagnosticProvider.SamplingOn,false);
            Assert.AreEqual(diagnosticProvider.SamplingRatePercentage,0);
           

            twin = new Twin();
            twin.Properties.Desired[BaseDiagnosticProvider.TwinDiagEnableKey] = "dddd";
            twin.Properties.Desired[BaseDiagnosticProvider.TwinDiagSamplingRateKey] = 10;

            diagnosticProvider.SetSamplingConfigFromTwin(twin.Properties.Desired);
            Assert.AreEqual(diagnosticProvider.SamplingOn,false);


            twin = new Twin();
            twin.Properties.Desired[BaseDiagnosticProvider.TwinDiagEnableKey] = true;
            twin.Properties.Desired[BaseDiagnosticProvider.TwinDiagSamplingRateKey] = "xxx";


            diagnosticProvider.SetSamplingConfigFromTwin(twin.Properties.Desired);
            Assert.AreEqual(diagnosticProvider.SamplingRatePercentage,0);
            

            twin = new Twin();
            twin.Properties.Desired[BaseDiagnosticProvider.TwinDiagEnableKey] = true;
            twin.Properties.Desired[BaseDiagnosticProvider.TwinDiagSamplingRateKey] = 10;
            diagnosticProvider.SetSamplingConfigFromTwin(twin.Properties.Desired);
        }


        [TestMethod]
        public void ThrowExceptionWhenSamplingRatePercentageOutOfRange()
        {

            var diagnosticProvider = new ContinuousDiagnosticProvider(SamplingRateSource.Client, 50);

            Assert.ThrowsException<SamplingPercentageOutOfRangeException>(() =>
            {
                diagnosticProvider = new ContinuousDiagnosticProvider(SamplingRateSource.Client, -1);
            });

            Assert.ThrowsException<SamplingPercentageOutOfRangeException>(() =>
            {
                diagnosticProvider = new ContinuousDiagnosticProvider(SamplingRateSource.Client, 101);
            });
        }

        [TestMethod]
        public void ThrowExceptionWhenUserMessageHasReservedProperty()
        {
            var diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Client, 100);

            Assert.ThrowsException<PropertyConflictException>(() =>
            {
                var message = new Message();
                message.Properties[BaseDiagnosticProvider.CorrelationIdKey] = "xxxxx";
                diagnosticProvider.Process(message);
            });

            Assert.ThrowsException<PropertyConflictException>(() =>
            {
                var message = new Message();
                message.Properties[BaseDiagnosticProvider.TimeStampKey] = "xxxxx";
                diagnosticProvider.Process(message);
            });

            Assert.ThrowsException<PropertyConflictException>(() =>
            {
                var message = new Message();
                message.Properties[BaseDiagnosticProvider.VersionKey] = "xxxxx";
                diagnosticProvider.Process(message);
            });
        }

        [TestMethod]
        public void ProbabilityDiagnosticProviderTest()
        {
            var diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Client, 100);
            for (var i = 0; i < 10000; i++)
            {
                Assert.AreEqual(diagnosticProvider.ShouldAddDiagnosticProperty(i), true);
            }

            diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Client, 0);
            for (var i = 0; i < 10000; i++)
            {
                Assert.AreEqual(diagnosticProvider.ShouldAddDiagnosticProperty(i), false);
            }

            diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Client, 50);
            var needSamplingCount = 0;
            for (var i = 0; i < 1000000; i++)
            {
                if (diagnosticProvider.ShouldAddDiagnosticProperty(i))
                {
                    needSamplingCount++;
                }
            }
            Trace.WriteLine("Need sampling count:" + needSamplingCount);
            var permissibleError = 0.2;
            Assert.IsTrue(needSamplingCount / 1000000.0 > 0.5 - permissibleError && needSamplingCount / 1000000.0 < 0.5 + permissibleError);

            diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Client, 25);
            needSamplingCount = 0;
            for (var i = 0; i < 1000000; i++)
            {
                if (diagnosticProvider.ShouldAddDiagnosticProperty(i))
                {
                    needSamplingCount++;
                }
            }
            Assert.IsTrue(needSamplingCount / 1000000.0 > 0.25 - permissibleError && needSamplingCount / 1000000.0 < 0.25 + permissibleError);

            diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Client, 20);
            needSamplingCount = 0;
            for (var i = 0; i < 1000000; i++)
            {
                if (diagnosticProvider.ShouldAddDiagnosticProperty(i))
                {
                    needSamplingCount++;
                }
            }
            Assert.IsTrue(needSamplingCount / 1000000.0 > 0.2 - permissibleError && needSamplingCount / 1000000.0 < 0.2 + permissibleError);

        }

        [TestMethod]
        public void ContinuousDiagnosticProviderTest()
        {

            var diagnosticProvider = new ContinuousDiagnosticProvider(SamplingRateSource.Client, 100);
            for (var i = 1; i <= 100; i++)
            {
                Assert.AreEqual(diagnosticProvider.ShouldAddDiagnosticProperty(i), true);
            }

            diagnosticProvider = new ContinuousDiagnosticProvider(SamplingRateSource.Client, 0);

            for (var i = 1; i <= 100; i++)
            {
                Assert.AreEqual(diagnosticProvider.ShouldAddDiagnosticProperty(i), false);
            }

            diagnosticProvider = new ContinuousDiagnosticProvider(SamplingRateSource.Client, 50);
            for (var i = 1; i <= 100; i++)
            {
                Assert.AreEqual(diagnosticProvider.ShouldAddDiagnosticProperty(i), i % 2 != 0);
            }

            diagnosticProvider = new ContinuousDiagnosticProvider(SamplingRateSource.Client, 25);

            for (var i = 1; i <= 100; i++)
            {
                Assert.AreEqual(diagnosticProvider.ShouldAddDiagnosticProperty(i), (i - 1) % 4 == 0);
            }

            diagnosticProvider = new ContinuousDiagnosticProvider(SamplingRateSource.Client, 20);

            for (var i = 1; i <= 100; i++)
            {
                Assert.AreEqual(diagnosticProvider.ShouldAddDiagnosticProperty(i), (i - 1) % 5 == 0);
            }
        }

        [TestMethod]
        public void ChangeSamplingRateWhenUseServerSamplingSourceAfterReceiveSettingsFromServer()
        {
            var diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Server);
            Assert.AreEqual(diagnosticProvider.SamplingRatePercentage, 0);
            var twin = new Twin();
            twin.Properties.Desired[BaseDiagnosticProvider.TwinDiagEnableKey] = "True";
            twin.Properties.Desired[BaseDiagnosticProvider.TwinDiagSamplingRateKey] = "10";
            diagnosticProvider.SetSamplingConfigFromTwin(twin.Properties.Desired);
            Assert.AreEqual(diagnosticProvider.SamplingRatePercentage, 10);
        }

        [TestMethod]
        public void DoNotChangeSamplingRateWhenUseClientSamlingSourceAfterReceiveSettingsFromServer()
        {
            var diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Server, 50);
            Assert.AreEqual(diagnosticProvider.SamplingRatePercentage, 0);
            var twin = new Twin();
            twin.Properties.Desired[BaseDiagnosticProvider.TwinDiagEnableKey] = "True";
            twin.Properties.Desired[BaseDiagnosticProvider.TwinDiagSamplingRateKey] = "10";
            diagnosticProvider.SetSamplingConfigFromTwin(twin.Properties.Desired);
            Assert.AreEqual(diagnosticProvider.SamplingRatePercentage, 10);
        }

        [TestMethod]
        public void SamplingSwitchIsNotOnWhenSamplingSourceIsNone()
        {
            var diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.None, 50);
            Assert.AreEqual(diagnosticProvider.SamplingOn, false);
        }

        [TestMethod]
        public void SamplingSwitchIsOnOnWhenSamplingSourceIsNotNone()
        {
            var diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Client, 50);
            Assert.AreEqual(diagnosticProvider.SamplingOn, true);
            diagnosticProvider = new ProbabilityDiagnosticProvider(SamplingRateSource.Server, 50);
            Assert.AreEqual(diagnosticProvider.SamplingOn, true);
        }

        [TestMethod]
        public void UpdateDiagnosticSettingsFromServer()
        {
            var diagnosticProvider = new ContinuousDiagnosticProvider(SamplingRateSource.Server);

            var twin = new Twin();
            twin.Properties.Desired[BaseDiagnosticProvider.TwinDiagEnableKey] = true;
            twin.Properties.Desired[BaseDiagnosticProvider.TwinDiagSamplingRateKey] = 10;

            diagnosticProvider.SetSamplingConfigFromTwin(twin.Properties.Desired);
            Assert.AreEqual(diagnosticProvider.SamplingOn, true);
            Assert.AreEqual(diagnosticProvider.SamplingRatePercentage, 10);
        }
    }
}
