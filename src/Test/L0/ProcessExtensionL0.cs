// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class ProcessExtensionL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task SuccessReadProcessEnv()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                string envName = Guid.NewGuid().ToString();
                string envValue = Guid.NewGuid().ToString();

                Process sleep = null;
                try
                {
                    // TODO: this was formerly skipped on Windows - why?
                    hc.EnqueueInstance<IProcessInvoker>(new ProcessInvokerWrapper());

                    // sleep 15 seconds
                    string sleepCmd = (TestUtil.IsWindows())
                        ? "powershell"
                        : "sleep";
                    string sleepArgs = (TestUtil.IsWindows())
                        ? "-Command \"Start-Sleep -s 15\""
                        : "15s";
                    var startInfo = new ProcessStartInfo(sleepCmd, sleepArgs);
                    startInfo.Environment[envName] = envValue;
                    sleep = Process.Start(startInfo);

                    var timeout = Process.GetProcessById(sleep.Id);
                    while (timeout == null)
                    {
                        await Task.Delay(1500);
                        timeout = Process.GetProcessById(sleep.Id);
                    }

                    try
                    {
                        await Task.Delay(2000);
                        trace.Info($"Read env from {timeout.Id}");
                        var value = timeout.GetEnvironmentVariable(hc, envName);
                        if (string.Equals(value, envValue, StringComparison.OrdinalIgnoreCase))
                        {
                            trace.Info($"Find the env.");
                            return;
                        }
                        else
                        {
                            Assert.True(false, "Expected environment '" + envValue + "' did not match actual '" + value + "'");
                        }
                    }
                    catch (Exception ex)
                    {
                        trace.Error(ex);
                        Assert.True(false, "Fail to retrive process environment variable due to exception: " + ex.Message + "\n" + ex.StackTrace);
                    }

                    Assert.True(false, "Fail to retrive process environment variable.");
                }
                finally
                {
                    sleep?.Kill();
                }
            }
        }
    }
}
