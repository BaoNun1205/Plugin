using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin
{
    public class Pl_E0002_02 : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace($"Plugin triggered on {context.MessageName}");

            Entity student = null;
            Entity preImage = null;
            Guid classId_New = Guid.Empty;
            Guid classId_Old = Guid.Empty;

            CalculateUpdateClass classUpdater = new CalculateUpdateClass(service);

            switch (context.MessageName.ToLower())
            {
                case "create":
                    if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                    {
                        student = (Entity)context.InputParameters["Target"];
                        if (student.Contains("ksvc_lup_class"))
                        {
                            classId_New = ((EntityReference)student["ksvc_lup_class"]).Id;
                            classUpdater.UpdateNumberOfStudents(classId_New, +1);
                        }
                    }
                    break;

                case "update":
                    if (context.PreEntityImages.Contains("PreImage"))
                    {
                        preImage = context.PreEntityImages["PreImage"];
                    }

                    if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                    {
                        student = (Entity)context.InputParameters["Target"];

                        if (student.Contains("ksvc_lup_class")) // Nếu class thay đổi
                        {
                            classId_New = ((EntityReference)student["ksvc_lup_class"]).Id;
                            if (preImage != null && preImage.Contains("ksvc_lup_class"))
                            {
                                classId_Old = ((EntityReference)preImage["ksvc_lup_class"]).Id;
                            }

                            if (classId_New != Guid.Empty) classUpdater.UpdateNumberOfStudents(classId_New, +1);
                            if (classId_Old != Guid.Empty && classId_New != classId_Old) classUpdater.UpdateNumberOfStudents(classId_Old, -1);
                        }
                    }
                    break;

                case "delete":
                    if (context.PreEntityImages.Contains("PreImage"))
                    {
                        preImage = context.PreEntityImages["PreImage"];
                        if (preImage.Contains("ksvc_lup_class"))
                        {
                            classId_Old = ((EntityReference)preImage["ksvc_lup_class"]).Id;
                            classUpdater.UpdateNumberOfStudents(classId_Old, -1);
                        }
                    }
                    break;
            }
        }
    }
}
