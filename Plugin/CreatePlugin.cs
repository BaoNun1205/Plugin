using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace Plugin
{
    public class CreatePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity entity)
            {
                // Obtain the IOrganizationService instance
                IOrganizationServiceFactory serviceFactory =
                    (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    tracingService.Trace("CreatePlugin execution started...");

                    // Check if the field ksvc_int_numberofstudent exists
                    if (entity.Attributes.Contains("ksvc_int_numberofstudent"))
                    {
                        int numberOfStudents = entity.GetAttributeValue<int>("ksvc_int_numberofstudent");
                        tracingService.Trace($"Number of students: {numberOfStudents}");

                        // If the number of students exceeds 50, throw an exception
                        if (numberOfStudents > 50)
                        {
                            tracingService.Trace("Number of students exceeds 50. Aborting record creation.");
                            throw new InvalidPluginExecutionException("Error: The number of students cannot exceed 50.");
                        }
                    }

                    // Create a task activity to follow up with the account customer in 7 days
                    Entity followup = new Entity("task")
                    {
                        ["subject"] = "Send e-mail to the new customer.",
                        ["description"] = "Follow up with the customer. Check if there are any new issues that need resolution.",
                        ["scheduledstart"] = DateTime.Now.AddDays(7),
                        ["scheduledend"] = DateTime.Now.AddDays(7),
                        ["category"] = context.PrimaryEntityName
                    };

                    // Refer to the account in the task activity
                    if (context.OutputParameters.Contains("ksvc_mst_classid"))
                    {
                        Guid regardingobjectid = new Guid(context.OutputParameters["ksvc_mst_classid"].ToString());
                        followup["regardingobjectid"] = new EntityReference("ksvc_mst_class", regardingobjectid);
                    }

                    // Create the task in Microsoft Dataverse
                    tracingService.Trace("FollowupPlugin: Creating the task activity.");
                    service.Create(followup);
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in FollowUpPlugin.", ex);
                }
                catch (Exception ex)
                {
                    tracingService.Trace("FollowUpPlugin: {0}", ex.ToString());
                    throw;
                }
            }
        }
    }
}
