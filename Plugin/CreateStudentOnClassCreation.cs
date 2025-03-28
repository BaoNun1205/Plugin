using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace Plugin
{
    public class CreateStudentOnClassCreation : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Tracing Service for debugging
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Execution Context
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Ensure this is a Create operation
            if (context.MessageName.ToLower() != "create")
            {
                tracingService.Trace("Not a create operation. Exiting...");
                return;
            }

            // Ensure Target entity exists
            if (!context.InputParameters.Contains("Target") ||
                !(context.InputParameters["Target"] is Entity entity))
            {
                tracingService.Trace("No target entity found. Exiting...");
                return;
            }

            // Obtain Organization Service
            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                tracingService.Trace("CreateStudentOnClassCreation started...");

                // Get the newly created Class ID
                Guid classId = entity.Id;
                tracingService.Trace($"New Class ID: {classId}");

                // Create a new Student linked to this Class
                Entity newStudent = new Entity("ksvc_tra_student");
                newStudent["ksvc_slt_studentname"] = "New Student";
                newStudent["ksvc_lup_class"] = new EntityReference("ksvc_mst_class", classId);
                newStudent["ksvc_opt_learningstatus"] = new OptionSetValue(1);

                // Create the Student record
                Guid studentId = service.Create(newStudent);
                tracingService.Trace($"Successfully created Student (ID: {studentId}) linked to Class {classId}");
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                tracingService.Trace($"OrganizationServiceFault: {ex.Message}");
                throw new InvalidPluginExecutionException("An error occurred in CreateStudentOnClassCreation.", ex);
            }
            catch (Exception ex)
            {
                tracingService.Trace($"General Exception: {ex}");
                throw;
            }
        }
    }
}
