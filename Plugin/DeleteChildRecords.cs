using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace Plugin
{
    public class DeleteChildRecords : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            tracingService.Trace("DeleteChildRecords Plugin started...");

            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (context.MessageName.ToLower() != "update" || context.Stage != 40)
            {
                tracingService.Trace("Not an Update operation in Post-Operation. Exiting...");
                return;
            }

            if (!context.PostEntityImages.Contains("PostImage") || !(context.PostEntityImages["PostImage"] is Entity postImage))
            {
                tracingService.Trace("No PostImage found. Exiting...");
                return;
            }

            if (!postImage.Contains("ksvc_opt_falcuty"))
            {
                tracingService.Trace("Field 'ksvc_opt_falcuty' was not updated. Exiting...");
                return;
            }

            Guid classId = postImage.Id;
            tracingService.Trace($"Class ID: {classId} - Faculty updated, proceeding to delete students...");

            try
            {
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                QueryExpression query = new QueryExpression("ksvc_tra_student")
                {
                    ColumnSet = new ColumnSet("ksvc_tra_studentid"),
                    Criteria =
                    {
                        Conditions =
                        {
                            new ConditionExpression("ksvc_lup_class", ConditionOperator.Equal, classId),
                            new ConditionExpression("ksvc_opt_learningstatus", ConditionOperator.Equal, 1)
                        }
                    }
                };

                EntityCollection students = service.RetrieveMultiple(query);
                tracingService.Trace($"Found {students.Entities.Count} inactive students for deletion.");

                if (students.Entities.Count == 0)
                {
                    tracingService.Trace("No students found for deletion. Exiting...");
                    return;
                }

                foreach (Entity student in students.Entities)
                {
                    Guid studentId = student.Id;

                    service.Delete("ksvc_tra_student", studentId);
                    tracingService.Trace($"Deleted Student ID: {studentId}");
                }

                tracingService.Trace("Deletion process completed successfully.");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Exception: {ex.Message}");
                throw new InvalidPluginExecutionException("An error occurred while deleting child records.", ex);
            }
        }
    }
}
