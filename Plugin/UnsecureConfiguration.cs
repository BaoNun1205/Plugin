using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin
{
    public class UnsecureConfiguration : IPlugin
    {
        private readonly string _configValue;

        public UnsecureConfiguration(string unsecureConfig, string secureConfig)
        {
            // Lưu giá trị từ Unsecure Configuration
            _configValue = unsecureConfig;

            if (string.IsNullOrWhiteSpace(_configValue))
            {
                throw new InvalidPluginExecutionException("❌ Unsecure Configuration is missing or empty.");
            }
        }
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            tracingService.Trace("🔹 DeleteChildRecords Plugin started...");

            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Chỉ thực thi khi có sự kiện Update và ở Post-Operation (Stage 40)
            if (context.MessageName.ToLower() != "update" || context.Stage != 40)
            {
                tracingService.Trace("ℹ️ Not an Update operation in Post-Operation. Exiting...");
                return;
            }

            if (!context.PostEntityImages.Contains("PostImage") || !(context.PostEntityImages["PostImage"] is Entity postImage))
            {
                tracingService.Trace("⚠️ No PostImage found. Exiting...");
                return;
            }

            if (!postImage.Contains("ksvc_opt_falcuty"))
            {
                tracingService.Trace("⚠️ Field 'ksvc_opt_falcuty' was not updated. Exiting...");
                return;
            }

            Guid classId = postImage.Id;
            tracingService.Trace($"📌 Class ID: {classId} - Faculty updated, proceeding to delete students...");
            tracingService.Trace($"📌 Loaded Config Value: {_configValue}"); // Debug giá trị lấy từ Unsecure Configuration

            try
            {
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                // Giá trị OptionSet của LearningStatus cho trạng thái Inactive (lấy từ Unsecure Config nếu có)
                int inactiveStatusValue;
                if (!int.TryParse(_configValue, out inactiveStatusValue))
                {
                    tracingService.Trace("⚠️ Invalid Unsecure Config Value. Using default: 1");
                    inactiveStatusValue = 1; // ⚠️ Giá trị mặc định nếu cấu hình không hợp lệ
                }

                QueryExpression query = new QueryExpression("ksvc_tra_student")
                {
                    ColumnSet = new ColumnSet("ksvc_tra_studentid"),
                    Criteria =
                    {
                        Conditions =
                        {
                            new ConditionExpression("ksvc_lup_class", ConditionOperator.Equal, classId),
                            new ConditionExpression("ksvc_opt_learningstatus", ConditionOperator.Equal, inactiveStatusValue) // ✅ Sử dụng giá trị từ cấu hình
                        }
                    }
                };

                EntityCollection students = service.RetrieveMultiple(query);
                tracingService.Trace($"🔍 Found {students.Entities.Count} inactive students for deletion.");

                if (students.Entities.Count == 0)
                {
                    tracingService.Trace("⚠️ No students found for deletion. Exiting...");
                    return;
                }

                foreach (Entity student in students.Entities)
                {
                    Guid studentId = student.Id;

                    // 🗑 Xóa sinh viên
                    service.Delete("ksvc_tra_student", studentId);
                    tracingService.Trace($"🗑 Deleted Student ID: {studentId}");
                }

                tracingService.Trace("✅ Deletion process completed successfully.");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"❌ Exception: {ex.Message}");
                throw new InvalidPluginExecutionException("An error occurred while deleting child records.", ex);
            }
        }
    }
}
