using Microsoft.AspNetCore.Mvc;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Services;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly ChatbotService _chatbotService;

        public ChatbotController()
        {
            _chatbotService = new ChatbotService();
        }

        public IActionResult Index()
        {
            // Truyền danh sách câu hỏi mẫu tới view
            ViewBag.SampleQuestions = _chatbotService.GetSampleQuestions();
            return View();
        }

        [HttpPost]
        public IActionResult SendMessage(string message)
        {
            string response = _chatbotService.ProcessUserMessage(message);
            
            // Truyền danh sách câu hỏi mẫu và phản hồi tới view
            ViewBag.SampleQuestions = _chatbotService.GetSampleQuestions();
            ViewBag.Response = response;
            
            return View("Index");
        }

        [HttpPost]
        public IActionResult SendSampleQuestion(string sampleQuestion)
        {
            string response = _chatbotService.ProcessUserMessage(sampleQuestion);
            
            // Truyền danh sách câu hỏi mẫu và phản hồi tới view
            ViewBag.SampleQuestions = _chatbotService.GetSampleQuestions();
            ViewBag.Response = response;
            
            return View("Index");
        }
    }
}