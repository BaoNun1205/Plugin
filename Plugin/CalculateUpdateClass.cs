using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin
{
    class CalculateUpdateClass
    {
        private readonly IOrganizationService _service;

        public CalculateUpdateClass(IOrganizationService service)
        {
            _service = service;
        }

        public void UpdateNumberOfStudents(Guid classId, int updateValue)
        {
            // Lấy thông tin lớp học từ classId
            Entity classEntity = _service.Retrieve("ksvc_mst_class", classId, new ColumnSet("ksvc_int_numberofstudent"));

            if (classEntity == null || !classEntity.Attributes.Contains("ksvc_int_numberofstudent"))
            {
                throw new InvalidOperationException("Không tìm thấy lớp học hoặc dữ liệu không hợp lệ.");
            }

            int currentNumber = classEntity.GetAttributeValue<int>("ksvc_int_numberofstudent");
            int newNumber = currentNumber + updateValue;

            // Kiểm tra nếu vượt quá 100 thì xuất lỗi
            if (newNumber > 100)
            {
                throw new InvalidOperationException("Lớp đã đạt tối đa 100 học sinh, không thể cập nhật thêm.");
            }

            // Cập nhật số lượng học sinh
            classEntity["ksvc_int_numberofstudent"] = newNumber;
            _service.Update(classEntity);
        }

        public decimal CalculateAverageScore(List<Entity> scores)
        {
            if (scores == null || scores.Count == 0)
            {
                throw new ArgumentException("Danh sách điểm số không được rỗng.");
            }

            Dictionary<int, decimal> weightFactors = new Dictionary<int, decimal>
            {
                { 1, 0.1m },
                { 2, 0.2m },
                { 3, 0.2m },
                { 4, 0.5m }
            };

            decimal totalScore = 0;
            decimal totalWeight = 0;

            foreach (var scoreEntity in scores)
            {
                if (!scoreEntity.Contains("ksvc_opt_examtype") || !scoreEntity.Contains("ksvc_dcn_scorenumber"))
                {
                    continue; // Bỏ qua nếu thiếu dữ liệu
                }

                int examType = scoreEntity.GetAttributeValue<OptionSetValue>("ksvc_opt_examtype").Value;
                decimal score = scoreEntity.GetAttributeValue<decimal>("ksvc_dcn_scorenumber");

                if (weightFactors.ContainsKey(examType))
                {
                    totalScore += score * weightFactors[examType];
                    totalWeight += weightFactors[examType];
                }
            }

            if (totalWeight == 0)
            {
                throw new InvalidOperationException("Không có trọng số hợp lệ để tính trung bình.");
            }

            return totalScore / totalWeight;
        }
    }
}
