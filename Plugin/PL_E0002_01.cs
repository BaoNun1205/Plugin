using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Plugin
{
    public class PL_E0002_01 : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            tracingService.Trace("Plugin PL_E0002_01 started execution.");

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity student)
            {
                Guid studentId = student.Id;
                int currentYear = DateTime.UtcNow.Year;

                DateTime startOfCurrentYear = new DateTime(currentYear, 1, 1);
                DateTime endOfCurrentYear = new DateTime(currentYear, 12, 31);
                DateTime startOfNextYear = new DateTime(currentYear + 1, 1, 1);
                DateTime endOfNextYear = new DateTime(currentYear + 1, 12, 31);

                // 1. Lấy danh sách semester
                QueryExpression semesterQuery = new QueryExpression("ksvc_mst_semester")
                {
                    ColumnSet = new ColumnSet("ksvc_mst_semesterid"),
                    Criteria = new FilterExpression(LogicalOperator.Or)
                    {
                        Filters =
                            {
                                new FilterExpression(LogicalOperator.And)
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("ksvc_opt_semetername", ConditionOperator.Equal, 1),
                                        new ConditionExpression("ksvc_dat_startdate", ConditionOperator.OnOrAfter, startOfCurrentYear),
                                        new ConditionExpression("ksvc_dat_startdate", ConditionOperator.OnOrBefore, endOfCurrentYear)
                                    }
                                },
                                new FilterExpression(LogicalOperator.And)
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("ksvc_opt_semetername", ConditionOperator.Equal, 2),
                                        new ConditionExpression("ksvc_dat_enddate", ConditionOperator.OnOrAfter, startOfNextYear),
                                        new ConditionExpression("ksvc_dat_enddate", ConditionOperator.OnOrBefore, endOfNextYear)
                                    }
                                }
                            }
                    }
                };

                tracingService.Trace("Retrieving semester records...");
                EntityCollection semesters = service.RetrieveMultiple(semesterQuery);
                tracingService.Trace($"Found {semesters.Entities.Count} semesters.");

                // 2. Lấy tất cả các Subject
                QueryExpression subjectQuery = new QueryExpression("ksvc_mst_subject")
                {
                    ColumnSet = new ColumnSet("ksvc_mst_subjectid")
                };

                tracingService.Trace("Retrieving subjects...");
                EntityCollection subjects = service.RetrieveMultiple(subjectQuery);
                tracingService.Trace($"Found {subjects.Entities.Count} subjects.");

                // 3. Tạo records trong ksvc_tra_subjectscore
                tracingService.Trace("Creating subject scores...");
                foreach (var semester in semesters.Entities)
                {
                    foreach (var subject in subjects.Entities)
                    {
                        Entity subjectScore = new Entity("ksvc_tra_subjectscore")
                        {
                            ["ksvc_lup_student"] = new EntityReference("ksvc_tra_student", studentId),
                            ["ksvc_lup_semester"] = semester.ToEntityReference(),
                            ["ksvc_lup_subject"] = subject.ToEntityReference()
                        };
                        service.Create(subjectScore);
                    }
                }
                tracingService.Trace("Subject scores created successfully.");

                // 4. Lấy danh sách subjectscore vừa tạo
                QueryExpression subjectScoreQuery = new QueryExpression("ksvc_tra_subjectscore")
                {
                    ColumnSet = new ColumnSet("ksvc_tra_subjectscoreid"),
                    Criteria = new FilterExpression()
                    {
                        Conditions =
                        {
                            new ConditionExpression("ksvc_lup_student", ConditionOperator.Equal, studentId)
                        }
                    }
                };

                tracingService.Trace("Retrieving created subject scores...");
                EntityCollection subjectScores = service.RetrieveMultiple(subjectScoreQuery);
                tracingService.Trace($"Found {subjectScores.Entities.Count} subject scores.");

                // 5. Tạo record trong ksvc_tra_score
                tracingService.Trace("Creating exam scores...");
                Random random = new Random();

                foreach (var subjectScore in subjectScores.Entities)
                {
                    for (int examType = 1; examType <= 4; examType++)
                    {
                        decimal randomScore = (decimal)random.Next(1, 11);

                        Entity score = new Entity("ksvc_tra_score")
                        {
                            ["ksvc_lup_subjectscore"] = subjectScore.ToEntityReference(),
                            ["ksvc_opt_examtype"] = new OptionSetValue(examType),
                            ["ksvc_dcn_scorenumber"] = randomScore
                        };

                        service.Create(score);
                        tracingService.Trace($"Created score: SubjectScoreId={subjectScore.Id}, ExamType={examType}, Score={randomScore}");
                    }
                }

                tracingService.Trace("Exam scores created successfully.");
                tracingService.Trace("Plugin PL_E0002_01 execution completed.");
            }
        }
    }
}
