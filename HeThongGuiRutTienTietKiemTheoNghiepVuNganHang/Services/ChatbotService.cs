using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Services
{
    public class ChatbotService
    {
        public string ProcessUserMessage(string message)
        {
            // Kiểm tra null hoặc chuỗi rỗng
            if (string.IsNullOrWhiteSpace(message))
            {
                return "Xin lỗi, tôi chưa hiểu câu hỏi của bạn. Bạn có thể chọn một trong các câu hỏi sau:\n1. Cách mở sổ tiết kiệm?\n2. Lãi suất tiết kiệm hiện tại?\n3. Các kỳ hạn gửi tiền?\n4. Cách gửi/rút tiền tiết kiệm?\n5. Thời gian làm việc của ngân hàng?\n6. Thông tin liên hệ hỗ trợ?";
            }
            
            // Chuyển đổi tin nhắn về chữ thường để dễ so sánh
            string lowerMessage = message.ToLower().Trim();

            // Kiểm tra các từ khóa và trả lời tương ứng
            if (lowerMessage.Contains("chào") || lowerMessage.Contains("xin chào") || lowerMessage.Contains("hello") || lowerMessage.Contains("hi"))
            {
                return "Xin chào! Tôi là chatbot hỗ trợ khách hàng của Hệ Thống Gửi Rút Tiền Tiết Kiệm Ngân Hàng. Bạn cần hỗ trợ gì?";
            }
            else if (lowerMessage.Contains("mở sổ") || lowerMessage.Contains("mo so") || lowerMessage.Contains("mở tài khoản tiết kiệm"))
            {
                return "Bạn có thể mở sổ tiết kiệm trực tiếp trên hệ thống:\n1. Truy cập Dashboard khách hàng\n2. Chọn chức năng 'Mở sổ tiết kiệm'\n3. Chọn gói tiết kiệm mong muốn từ danh sách\n4. Nhập số tiền gửi (tối thiểu 1.000.000 VNĐ)\n5. Chọn kỳ hạn phù hợp\n6. Xác nhận thông tin và đồng ý mở sổ\n7. Hệ thống sẽ tạo sổ tiết kiệm và cập nhật số dư\n\nNgoài ra, bạn cũng có thể đến quầy giao dịch với CMND/CCCD bản gốc để được nhân viên hỗ trợ.";
            }
            else if (lowerMessage.Contains("gửi tiền") || lowerMessage.Contains("rut tien") || lowerMessage.Contains("rút tiền") || lowerMessage.Contains("gửi/rút"))
            {
                return "Bạn có thể gửi/rút tiền tiết kiệm trực tiếp trên hệ thống:\n1. Truy cập Dashboard khách hàng\n2. Chọn sổ tiết kiệm muốn giao dịch\n3. Chọn chức năng 'Gửi/Rút tiền'\n4. Nhập số tiền và chọn loại giao dịch\n5. Xác nhận giao dịch\n6. Hệ thống sẽ xử lý và cập nhật số dư ngay lập tức\n\nLưu ý:\n- Chỉ có thể rút tiền nếu số dư sau rút >= 1.000.000 VNĐ\n- Rút trước hạn sẽ áp dụng lãi suất không kỳ hạn\n- Gửi tiền không giới hạn số lần";
            }
            else if (lowerMessage.Contains("lãi suất") || lowerMessage.Contains("lai suat"))
            {
                return "Lãi suất tiết kiệm của ngân hàng:\n- Kỳ hạn 1 tháng: 4.5%/năm\n- Kỳ hạn 3 tháng: 5.0%/năm\n- Kỳ hạn 6 tháng: 5.5%/năm\n- Kỳ hạn 12 tháng: 6.5%/năm\n- Kỳ hạn 24 tháng: 7.0%/năm\nLãi suất có thể thay đổi theo thời điểm, vui lòng kiểm tra lại trên Dashboard.";
            }
            else if (lowerMessage.Contains("kỳ hạn") || lowerMessage.Contains("ky han"))
            {
                return "Các kỳ hạn gửi tiền tiết kiệm hiện có:\n1. 1 tháng - Lãi suất 4.5%/năm\n2. 3 tháng - Lãi suất 5.0%/năm\n3. 6 tháng - Lãi suất 5.5%/năm\n4. 12 tháng - Lãi suất 6.5%/năm\n5. 24 tháng - Lãi suất 7.0%/năm\nBạn có thể chọn kỳ hạn phù hợp với nhu cầu của mình.";
            }
            else if (lowerMessage.Contains("đóng sổ") || lowerMessage.Contains("dong so") || lowerMessage.Contains("tất toán"))
            {
                return "Bạn có thể tất toán sổ tiết kiệm trực tiếp trên hệ thống:\n1. Truy cập Dashboard khách hàng\n2. Chọn sổ tiết kiệm muốn tất toán\n3. Xem chi tiết sổ tiết kiệm\n4. Chọn chức năng 'Tất toán'\n5. Xác nhận thông tin và đồng ý tất toán\n6. Hệ thống sẽ xử lý và chuyển số tiền gốc + lãi về tài khoản ngân hàng của bạn\n\nLưu ý:\n- Nếu tất toán trước hạn: Áp dụng lãi suất không kỳ hạn (0.5%/năm)\n- Nếu đáo hạn: Được hưởng lãi suất theo kỳ hạn đã chọn\n- Thời gian xử lý: Ngay lập tức sau khi xác nhận";
            }
            else if (lowerMessage.Contains("thời gian") || lowerMessage.Contains("gio lam viec") || lowerMessage.Contains("giờ làm việc"))
            {
                return "Giờ làm việc của ngân hàng:\n- Thứ 2 đến Thứ 6: 7h30 - 16h30\n- Thứ 7: 7h30 - 11h30\n- Chủ nhật và ngày lễ: Nghỉ\nQuý khách có thể thực hiện giao dịch online 24/7 thông qua Internet Banking.";
            }
            else if (lowerMessage.Contains("liên hệ") || lowerMessage.Contains("lien he") || lowerMessage.Contains("số điện thoại") || lowerMessage.Contains("so dien thoai"))
            {
                return "Thông tin liên hệ hỗ trợ:\n- Tổng đài chăm sóc khách hàng: 1900 1234\n- Email hỗ trợ: hotro@nganhang.com\n- Trụ sở chính: 123 Đường ABC, Quận XYZ, TP. HCM\n- Website: www.nganhang.com";
            }
            else if (lowerMessage.Contains("dashboard") || lowerMessage.Contains("bảng điều khiển"))
            {
                return "Dashboard khách hàng bao gồm:\n1. Thông tin tài khoản ngân hàng\n2. Danh sách sổ tiết kiệm\n3. Lịch sử giao dịch gần đây\n4. Thông báo từ ngân hàng\n\nTừ Dashboard, bạn có thể thực hiện các thao tác:\n- Mở sổ tiết kiệm mới\n- Gửi/Rút tiền từ sổ tiết kiệm\n- Tất toán sổ tiết kiệm\n- Xem chi tiết từng sổ tiết kiệm\n- Xem lịch sử giao dịch\n- Cập nhật thông tin cá nhân";
            }
            else if (lowerMessage.Contains("cảm ơn") || lowerMessage.Contains("cam on") || lowerMessage.Contains("thank"))
            {
                return "Rất hân hạnh được hỗ trợ bạn! Nếu còn câu hỏi nào khác, đừng ngần ngại hỏi nhé!";
            }
            else
            {
                // Trả lời mặc định nếu không tìm thấy từ khóa phù hợp
                return "Xin lỗi, tôi chưa hiểu câu hỏi của bạn. Bạn có thể chọn một trong các câu hỏi sau:\n1. Cách mở sổ tiết kiệm?\n2. Lãi suất tiết kiệm hiện tại?\n3. Các kỳ hạn gửi tiền?\n4. Cách gửi/rút tiền tiết kiệm?\n5. Thời gian làm việc của ngân hàng?\n6. Thông tin liên hệ hỗ trợ?";
            }
        }

        public List<string> GetSampleQuestions()
        {
            return new List<string>
            {
                "Cách mở sổ tiết kiệm trên hệ thống?",
                "Lãi suất tiết kiệm các kỳ hạn?",
                "Cách gửi/rút tiền tiết kiệm?",
                "Cách tất toán sổ tiết kiệm?",
                "Chức năng Dashboard khách hàng?",
                "Thông tin liên hệ hỗ trợ?"
            };
        }
    }
}