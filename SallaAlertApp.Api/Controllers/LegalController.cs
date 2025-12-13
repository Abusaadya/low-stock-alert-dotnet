using Microsoft.AspNetCore.Mvc;

namespace SallaAlertApp.Api.Controllers;

[Route("")]
public class LegalController : BaseController
{
    [HttpGet("privacy")]
    public IActionResult Privacy()
    {
        var html = @"<!DOCTYPE html>
<html lang=""ar"" dir=""rtl"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>سياسة الخصوصية - منبه المخزون</title>
    <style>
        body { font-family: 'Tajawal', sans-serif; line-height: 1.6; padding: 20px; max-width: 800px; margin: 0 auto; color: #333; }
        h1, h2 { color: #004d40; }
        .container { background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
    </style>
    <link href=""https://fonts.googleapis.com/css2?family=Tajawal:wght@400;700&display=swap"" rel=""stylesheet"">
</head>
<body>
    <div class=""container"">
        <h1>سياسة الخصوصية</h1>
        <p>آخر تحديث: 2025-12-14</p>
        
        <h2>1. المقدمة</h2>
        <p>نحن في ""منبه المخزون"" نحرص على حماية خصوصية بياناتك. تشرح هذه السياسة كيفية جمعنا واستخدامنا وحمايتنا لمعلوماتك الشخصية عند استخدامك لتطبيقنا.</p>
        
        <h2>2. المعلومات التي نجمعها</h2>
        <ul>
            <li><strong>معلومات التاجر:</strong> الاسم، البريد الإلكتروني، ورقم الهاتف كما هو مزود من منصة سلة.</li>
            <li><strong>بيانات المتجر:</strong> معلومات المنتجات والمخزون لغرض إرسال التنبيهات.</li>
        </ul>

        <h2>3. كيفية استخدام المعلومات</h2>
        <p>نستخدم المعلومات لـ:</p>
        <ul>
            <li>تقديم خدمة التنبيهات عند انخفاض المخزون.</li>
            <li>تحسين أداء التطبيق وتجربة المستخدم.</li>
            <li>التواصل معك بخصوص التحديثات أو الدعم الفني.</li>
        </ul>

        <h2>4. مشاركة البيانات</h2>
        <p>نحن لا نبيع أو نؤجر بياناتك الشخصية لأي طرف ثالث. قد نشارك البيانات فقط مع:</p>
        <ul>
            <li>مقدمي الخدمات التقنية (مثل خدمات الاستضافة) لتشغيل التطبيق.</li>
            <li>الجهات القانونية إذا طلب منا ذلك بموجب القانون.</li>
        </ul>

        <h2>5. الأمان</h2>
        <p>نتخذ تدابير أمنية مناسبة لحماية بياناتك من الوصول غير المصرح به أو التغيير أو الإفصاح أو الإتلاف.</p>

        <h2>6. حذف البيانات</h2>
        <p>عند إلغاء تثبيت التطبيق، يتم حذف بيانات التاجر واشتراكه من سجلاتنا النشطة.</p>

        <h2>7. اتصل بنا</h2>
        <p>إذا كان لديك أي أسئلة حول سياسة الخصوصية، يرجى التواصل معنا.</p>
    </div>
</body>
</html>";
        return Content(html, "text/html");
    }

    [HttpGet("terms")]
    public IActionResult Terms()
    {
        var html = @"<!DOCTYPE html>
<html lang=""ar"" dir=""rtl"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>شروط الخدمة - منبه المخزون</title>
    <style>
        body { font-family: 'Tajawal', sans-serif; line-height: 1.6; padding: 20px; max-width: 800px; margin: 0 auto; color: #333; }
        h1, h2 { color: #004d40; }
        .container { background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
    </style>
    <link href=""https://fonts.googleapis.com/css2?family=Tajawal:wght@400;700&display=swap"" rel=""stylesheet"">
</head>
<body>
    <div class=""container"">
        <h1>شروط الخدمة</h1>
        <p>آخر تحديث: 2025-12-14</p>
        
        <h2>1. قبول الشروط</h2>
        <p>بتحميلك أو استخدامك لتطبيق ""منبه المخزون""، فإنك توافق على الالتزام بشروط الخدمة هذه.</p>
        
        <h2>2. وصف الخدمة</h2>
        <p>يقدم التطبيق خدمة إرسال تنبيهات للتجار عبر قنوات مختلفة (مثل البريد الإلكتروني أو تليجرام) عندما يصل مخزون المنتجات إلى حد معين.</p>

        <h2>3. التزامات المستخدم</h2>
        <p>أنت توافق على:</p>
        <ul>
            <li>تقديم معلومات صحيحة ودقيقة عند التسجيل.</li>
            <li>عدم استخدام الخدمة لأي أغراض غير قانونية أو غير مصرح بها.</li>
            <li>الحفاظ على سرية بيانات حسابك.</li>
        </ul>

        <h2>4. الملكية الفكرية</h2>
        <p>جميع حقوق الملكية الفكرية المتعلقة بالتطبيق ومحتواه محفوظة لنا.</p>

        <h2>5. حدود المسؤولية</h2>
        <p>نحن نسعى لتقديم خدمة موثوقة، ولكننا لا نضمن خلو الخدمة من الأخطاء أو الانقطاعات. نحن غير مسؤولين عن أي خسائر ناتجة عن استخدامك للخدمة أو عدم قدرتك على استخدامها.</p>

        <h2>6. التعديلات</h2>
        <p>نحتفظ بالحق في تعديل هذه الشروط في أي وقت. سيتم إشعارك بأي تغييرات جوهرية.</p>

        <h2>7. الإنهاء</h2>
        <p>يحق لنا إنهاء أو تعليق وصولك للخدمة في حال مخالفتك لهذه الشروط.</p>
    </div>
</body>
</html>";
        return Content(html, "text/html");
    }
}
