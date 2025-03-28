using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace Plugin
{
    public class UpdatePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Only process if it's an Update event
            if (context.MessageName.ToLower() != "update")
            {
                tracingService.Trace("Not an update operation. Exiting...");
                return;
            }

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
                    tracingService.Trace("Processing update plugin...");

                    // Get PreImage (before update)
                    Entity preImage = null;
                    if (context.PreEntityImages.Contains("PreImageData") && context.PreEntityImages["PreImageData"] is Entity)
                    {
                        preImage = context.PreEntityImages["PreImageData"];
                        tracingService.Trace("PreImage retrieved successfully.");
                        LogEntityAttributes(preImage, "PreImage", tracingService);
                    }
                    else
                    {
                        tracingService.Trace("No PreImage found.");
                    }

                    // Get PostImage (after update)
                    Entity postImage = null;
                    if (context.PostEntityImages.Contains("PostImageData") && context.PostEntityImages["PostImageData"] is Entity)
                    {
                        postImage = context.PostEntityImages["PostImageData"];
                        tracingService.Trace("PostImage retrieved successfully.");
                        LogEntityAttributes(postImage, "PostImage", tracingService);
                    }
                    else
                    {
                        tracingService.Trace("No PostImage found.");
                    }

                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    tracingService.Trace($"OrganizationServiceFault: {ex.Message}");
                    throw new InvalidPluginExecutionException("An error occurred in UpdatePlugin.", ex);
                }
                catch (Exception ex)
                {
                    tracingService.Trace($"General Exception: {ex}");
                    throw;
                }
            }
            else
            {
                tracingService.Trace("No target entity found. Exiting...");
            }
        }

        // Helper method to log entity attributes
        private void LogEntityAttributes(Entity entity, string imageType, ITracingService tracingService)
        {
            tracingService.Trace($"{imageType} attributes:");
            foreach (var attr in entity.Attributes)
            {
                if (attr.Value is OptionSetValue optionSet)
                {
                    tracingService.Trace($"    - {attr.Key}: {optionSet.Value} (OptionSetValue)");
                }
                else if (attr.Value is EntityReference entityRef)
                {
                    tracingService.Trace($"    - {attr.Key}: {entityRef.LogicalName} ({entityRef.Id}) (EntityReference)");
                }
                else
                {
                    tracingService.Trace($"    - {attr.Key}: {attr.Value}");
                }
            }
        }
    }
}
