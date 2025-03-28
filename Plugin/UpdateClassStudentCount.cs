using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Plugin
{
    public class UpdateClassStudentCount : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            tracingService.Trace("UpdateClassStudentCount started...");

            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.MessageName.ToLower() != "create")
            {
                tracingService.Trace("Not a Create operation. Exiting...");
                return;
            }

            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity studentEntity))
            {
                tracingService.Trace("No target entity found. Exiting...");
                return;
            }

            if (!studentEntity.Attributes.Contains("ksvc_lup_class"))
            {
                tracingService.Trace("No ksvc_lup_class found in student. Exiting...");
                return;
            }

            EntityReference classRef = studentEntity.GetAttributeValue<EntityReference>("ksvc_lup_class");
            Guid classId = classRef.Id;
            tracingService.Trace($"Student belongs to Class ID: {classId}");

            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                ColumnSet columns = new ColumnSet("ksvc_int_numberofstudent");
                Entity classEntity = service.Retrieve("ksvc_mst_class", classId, columns);

                if (classEntity == null)
                {
                    tracingService.Trace($"Class with ID {classId} not found.");
                    return;
                }

                int currentStudentCount = classEntity.Contains("ksvc_int_numberofstudent")
                    ? classEntity.GetAttributeValue<int>("ksvc_int_numberofstudent")
                    : 0;

                int newStudentCount = currentStudentCount + 1;
                tracingService.Trace($"Updating student count: {currentStudentCount} ➝ {newStudentCount}");

                Entity updateClass = new Entity("ksvc_mst_class")
                {
                    Id = classId
                };
                updateClass["ksvc_int_numberofstudent"] = newStudentCount;

                service.Update(updateClass);
                tracingService.Trace("Class student count updated successfully.");
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                tracingService.Trace($"OrganizationServiceFault: {ex.Message}");
                throw new InvalidPluginExecutionException("An error occurred in UpdateClassStudentCount.", ex);
            }
            catch (Exception ex)
            {
                tracingService.Trace($"General Exception: {ex}");
                throw;
            }
        }
    }
}
