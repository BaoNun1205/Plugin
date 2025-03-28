using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace Plugin
{
    public class SetDefaultStudentCount : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            tracingService.Trace("SetDefaultStudentCount PreOperation Plugin started...");

            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.Stage != 20)
            {
                tracingService.Trace("Not a Pre-Operation stage. Exiting...");
                return;
            }

            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity targetEntity))
            {
                tracingService.Trace("No target entity found. Exiting...");
                return;
            }

            try
            {
                if (targetEntity.LogicalName != "ksvc_mst_class")
                {
                    tracingService.Trace("Target entity is not 'ksvc_mst_class'. Exiting...");
                    return;
                }

                tracingService.Trace("Setting 'ksvc_int_numberofstudent' to 0...");
                targetEntity["ksvc_int_numberofstudent"] = 0;

                tracingService.Trace("Default student count set successfully.");
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                tracingService.Trace($"OrganizationServiceFault: {ex.Message}");
                throw new InvalidPluginExecutionException("An error occurred in SetDefaultStudentCount.", ex);
            }
            catch (Exception ex)
            {
                tracingService.Trace($"General Exception: {ex}");
                throw;
            }
        }
    }
}
