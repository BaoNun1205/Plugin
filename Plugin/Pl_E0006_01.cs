using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin
{
    public class Pl_E0006_01 : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            CalculateUpdateClass classUpdater = new CalculateUpdateClass(service);

            if (context.MessageName.ToLower() != "update" || !context.PreEntityImages.Contains("PreImage"))
            {
                return;
            }

            Entity preImage = context.PreEntityImages["PreImage"];
            Entity target = (Entity)context.InputParameters["Target"];

            if (!preImage.Contains("ksvc_dcn_scorenumber") || !target.Contains("ksvc_dcn_scorenumber"))
            {
                return;
            }

            tracingService.Trace("Score number changed, processing...");

            if (!preImage.Contains("ksvc_lup_subjectscore"))
            {
                return;
            }

            EntityReference subjectScoreRef = preImage.GetAttributeValue<EntityReference>("ksvc_lup_subjectscore");
            Guid subjectScoreId = subjectScoreRef.Id;

            QueryExpression query = new QueryExpression("ksvc_tra_score")
            {
                ColumnSet = new ColumnSet("ksvc_opt_examtype", "ksvc_dcn_scorenumber"),
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition("ksvc_lup_subjectscore", ConditionOperator.Equal, subjectScoreId);

            EntityCollection scoreRecords = service.RetrieveMultiple(query);

            if (scoreRecords.Entities.Count == 4)
            {
                decimal averageScore = classUpdater.CalculateAverageScore(scoreRecords.Entities.ToList());
                tracingService.Trace("Calculated average score: " + averageScore);

                Entity updateSubjectScore = new Entity("ksvc_tra_subjectscore")
                {
                    Id = subjectScoreId,
                    ["ksvc_dcn_averagescore"] = averageScore
                };

                service.Update(updateSubjectScore);
                tracingService.Trace("Updated average score for subjectscore: " + subjectScoreId);
            }
            else
            {
                tracingService.Trace("Not enough exam types (4 required), skipping average score update.");
            }
        }
    }
}
