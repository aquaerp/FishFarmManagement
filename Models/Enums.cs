using System.ComponentModel.DataAnnotations;

namespace FishFarmManager.Models
{
    /// <summary>
    /// حالة الحضور والغياب للموظفين
    /// Attendance status for employees
    /// </summary>
    public enum AttendanceStatus
    {
        [Display(Name = "حاضر")]
        Present = 1,
        
        [Display(Name = "غائب")]
        Absent = 2,
        
        [Display(Name = "إجازة")]
        Leave = 3,
        
        [Display(Name = "في إجازة")]
        OnLeave = 4,
        
        [Display(Name = "مريض")]
        Sick = 5,
        
        [Display(Name = "تأخير")]
        Late = 6,
        
        [Display(Name = "مستأذن")]
        Excused = 7,
        
        [Display(Name = "عطلة رسمية")]
        Holiday = 8,
        
        [Display(Name = "يوم راحة")]
        DayOff = 9
    }

    /// <summary>
    /// مناصب الموظفين
    /// Employee positions
    /// </summary>
    public enum EmployeePosition
    {
        [Display(Name = "مدير")]
        Manager = 1,
        
        [Display(Name = "مساعد مدير")]
        Assistant_Manager = 2,
        
        [Display(Name = "مشرف")]
        Supervisor = 3,
        
        [Display(Name = "مهندس")]
        Engineer = 4,
        
        [Display(Name = "فني")]
        Technician = 5,
        
        [Display(Name = "عامل")]
        Worker = 6,
        
        [Display(Name = "محاسب")]
        Accountant = 7,
        
        [Display(Name = "سائق")]
        Driver = 8,
        
        [Display(Name = "حارس")]
        Guard = 9,
        
        [Display(Name = "حارس")]
        Security = 10,
        
        [Display(Name = "سكرتير")]
        Secretary = 11,
        
        [Display(Name = "صيانة")]
        Maintenance = 12,
        
        [Display(Name = "مراقبة جودة")]
        Quality_Control = 13,
        
        [Display(Name = "مبيعات")]
        Sales = 14,
        
        [Display(Name = "أخرى")]
        Other = 99
    }

    /// <summary>
    /// أقسام الموظفين
    /// Employee departments
    /// </summary>
    public enum EmployeeDepartment
    {
        [Display(Name = "الإدارة")]
        Management = 1,
        
        [Display(Name = "الإنتاج")]
        Production = 2,
        
        [Display(Name = "المالية")]
        Finance = 3,
        
        [Display(Name = "المحاسبة")]
        Accounting = 4,
        
        [Display(Name = "المبيعات")]
        Sales = 5,
        
        [Display(Name = "المشتريات")]
        Purchasing = 6,
        
        [Display(Name = "التقنية")]
        Technical = 7,
        
        [Display(Name = "الأمن")]
        Security = 8,
        
        [Display(Name = "الصيانة")]
        Maintenance = 9,
        
        [Display(Name = "مراقبة الجودة")]
        Quality_Control = 10,
        
        [Display(Name = "اللوجستيات")]
        Logistics = 11,
        
        [Display(Name = "أخرى")]
        Other = 99
    }

    /// <summary>
    /// نوع التوظيف
    /// Employment type
    /// </summary>
    public enum EmploymentType
    {
        [Display(Name = "دائم")]
        Permanent = 1,
        
        [Display(Name = "دوام كامل")]
        FullTime = 2,
        
        [Display(Name = "مؤقت")]
        Temporary = 3,
        
        [Display(Name = "عقد")]
        Contract = 4,
        
        [Display(Name = "جزئي")]
        PartTime = 5,
        
        [Display(Name = "يومي")]
        Daily = 6,
        
        [Display(Name = "موسمي")]
        Seasonal = 7
    }

    /// <summary>
    /// حالة الموظف
    /// Employee status
    /// </summary>
    public enum EmployeeStatus
    {
        [Display(Name = "نشط")]
        Active = 1,
        
        [Display(Name = "غير نشط")]
        Inactive = 2,
        
        [Display(Name = "في إجازة")]
        OnLeave = 3,
        
        [Display(Name = "موقوف")]
        Suspended = 4,
        
        [Display(Name = "استقال")]
        Resigned = 5,
        
        [Display(Name = "فصل")]
        Terminated = 6
    }

    /// <summary>
    /// نوع الإجازة
    /// Leave type
    /// </summary>
    public enum LeaveType
    {
        [Display(Name = "إجازة سنوية")]
        Annual = 1,
        
        [Display(Name = "إجازة مرضية")]
        Sick = 2,
        
        [Display(Name = "إجازة شخصية")]
        Personal = 3,
        
        [Display(Name = "إجازة أمومة")]
        Maternity = 4,
        
        [Display(Name = "إجازة أبوية")]
        Paternity = 5,
        
        [Display(Name = "إجازة طوارئ")]
        Emergency = 6,
        
        [Display(Name = "إجازة بدون راتب")]
        Unpaid = 7,
        
        [Display(Name = "إجازة حج")]
        Hajj = 8,
        
        [Display(Name = "إجازة زواج")]
        Marriage = 9,
        
        [Display(Name = "إجازة عزاء")]
        Bereavement = 10,
        
        [Display(Name = "إجازة دراسية")]
        Study = 11,
        
        [Display(Name = "إجازة أخرى")]
        Other = 99
    }

    /// <summary>
    /// حالة الإجازة
    /// Leave status
    /// </summary>
    public enum LeaveStatus
    {
        [Display(Name = "معلقة")]
        Pending = 1,
        
        [Display(Name = "موافقة")]
        Approved = 2,
        
        [Display(Name = "مرفوضة")]
        Rejected = 3,
        
        [Display(Name = "مسترجعة")]
        Cancelled = 4
    }

    /// <summary>
    /// فئات التكلفة
    /// Cost categories
    /// </summary>
    public enum CostCategory
    {
        [Display(Name = "أعلاف")]
        Feed = 1,
        
        [Display(Name = "زريعة")]
        Fingerlings = 2,
        
        [Display(Name = "أدوية")]
        Medicine = 3,
        
        [Display(Name = "دواء")]
        Medication = 4,
        
        [Display(Name = "فيتامينات")]
        Vitamins = 5,
        
        [Display(Name = "مياه")]
        Water = 6,
        
        [Display(Name = "أكسجين")]
        Oxygen = 7,
        
        [Display(Name = "وقود")]
        Fuel = 8,
        
        [Display(Name = "طاقة")]
        Energy = 9,
        
        [Display(Name = "كهرباء")]
        Electricity = 10,
        
        [Display(Name = "عمالة")]
        Labor = 11,
        
        [Display(Name = "عمالة إنتاج")]
        Labor_Production = 12,
        
        [Display(Name = "عمالة صيانة")]
        Labor_Maintenance = 13,
        
        [Display(Name = "عمالة إدارية")]
        Labor_Management = 14,
        
        [Display(Name = "صيانة")]
        Maintenance = 15,
        
        [Display(Name = "صيانة البرك")]
        Pond_Maintenance = 16,
        
        [Display(Name = "صيانة المعدات")]
        Equipment_Maintenance = 17,
        
        [Display(Name = "معدات")]
        Equipment = 18,
        
        [Display(Name = "شراء معدات")]
        Equipment_Purchase = 19,
        
        [Display(Name = "إنشاء برك")]
        Pond_Construction = 20,
        
        [Display(Name = "نقل")]
        Transportation = 21,
        
        [Display(Name = "إيجار")]
        Rent = 22,
        
        [Display(Name = "تأمين")]
        Insurance = 23,
        
        [Display(Name = "ضرائب ورسوم")]
        Taxes_Fees = 24,
        
        [Display(Name = "شهادات")]
        Certification = 25,
        
        [Display(Name = "تعبئة وتغليف")]
        Packaging = 26,
        
        [Display(Name = "تسويق")]
        Marketing = 27,
        
        [Display(Name = "فحوصات وتحليل")]
        Testing_Analysis = 28,
        
        [Display(Name = "مواد كيميائية")]
        Chemicals = 29,
        
        [Display(Name = "استشارات")]
        Consulting = 30,
        
        [Display(Name = "أخرى")]
        Other = 99
    }

    /// <summary>
    /// حالة البركة
    /// Pond status
    /// </summary>
    public enum PondStatus
    {
        [Display(Name = "فارغة")]
        Empty = 1,
        
        [Display(Name = "مجهزة")]
        Prepared = 2,
        
        [Display(Name = "قيد التجهيز")]
        Preparation = 3,
        
        [Display(Name = "مستزرعة")]
        Stocked = 4,
        
        [Display(Name = "نشطة")]
        Active = 5,
        
        [Display(Name = "تحت الصيانة")]
        Maintenance = 6,
        
        [Display(Name = "غير متاحة")]
        Unavailable = 7
    }

    /// <summary>
    /// نوع البركة
    /// Pond type
    /// </summary>
    public enum PondType
    {
        [Display(Name = "تربية")]
        Nursery = 1,
        
        [Display(Name = "نمو")]
        GrowOut = 2,
        
        [Display(Name = "تفريخ")]
        Breeding = 3,
        
        [Display(Name = "مفرخ")]
        Hatchery = 4,
        
        [Display(Name = "عزل")]
        Quarantine = 5
    }

    /// <summary>
    /// حالة جودة المياه
    /// Water quality status
    /// </summary>
    public enum WaterQualityStatus
    {
        [Display(Name = "ممتاز")]
        Excellent = 1,
        
        [Display(Name = "جيد")]
        Good = 2,
        
        [Display(Name = "مقبول")]
        Acceptable = 3,
        
        [Display(Name = "تحذير")]
        Warning = 4,
        
        [Display(Name = "ضعيف")]
        Poor = 5,
        
        [Display(Name = "خطير")]
        Critical = 6
    }

    /// <summary>
    /// حالة الراتب
    /// Salary status
    /// </summary>
    public enum SalaryStatus
    {
        [Display(Name = "مسودة")]
        Draft = 0,
        
        [Display(Name = "معلق")]
        Pending = 1,
        
        [Display(Name = "معتمد")]
        Approved = 2,
        
        [Display(Name = "مدفوع")]
        Paid = 3,
        
        [Display(Name = "متأخر")]
        Overdue = 4,
        
        [Display(Name = "ملغي")]
        Cancelled = 5
    }

    /// <summary>
    /// طريقة الدفع
    /// Payment method
    /// </summary>
    public enum PaymentMethod
    {
        [Display(Name = "نقدي")]
        Cash = 1,
        
        [Display(Name = "تحويل بنكي")]
        BankTransfer = 2,
        
        [Display(Name = "شيك")]
        Check = 3,
        
        [Display(Name = "دفع آجل")]
        Credit = 4
    }

    /// <summary>
    /// حالة دورة الإنتاج
    /// Production cycle status
    /// </summary>
    public enum CycleStatus
    {
        [Display(Name = "تخطيط")]
        Planning = 0,
        
        [Display(Name = "مخططة")]
        Planned = 1,
        
        [Display(Name = "جارية")]
        Active = 2,
        
        [Display(Name = "مكتملة")]
        Completed = 3,
        
        [Display(Name = "ملغية")]
        Cancelled = 4,
        
        [Display(Name = "معلقة")]
        Suspended = 5,
        
        [Display(Name = "منتهية")]
        Terminated = 6
    }

    /// <summary>
    /// نوع دورة الإنتاج
    /// Production cycle type
    /// </summary>
    public enum CycleType
    {
        [Display(Name = "دورة كاملة")]
        FullCycle = 1,
        
        [Display(Name = "تربية")]
        Nursery = 2,
        
        [Display(Name = "نمو")]
        GrowOut = 3,
        
        [Display(Name = "تفريخ")]
        Breeding = 4,
        
        [Display(Name = "مفرخ")]
        Hatchery = 5
    }

    /// <summary>
    /// درجات الأسماك
    /// Fish grades
    /// </summary>
    public enum FishGrade
    {
        [Display(Name = "ممتاز")]
        Premium = 0,
        
        [Display(Name = "درجة أولى")]
        GradeA = 1,
        
        [Display(Name = "درجة ثانية")]
        GradeB = 2,
        
        [Display(Name = "درجة ثالثة")]
        GradeC = 3,
        
        [Display(Name = "غير مصنف")]
        Ungraded = 4,
        
        [Display(Name = "مرفوض")]
        Rejected = 5
    }

    /// <summary>
    /// نوع التكلفة
    /// Cost type
    /// </summary>
    public enum CostType
    {
        [Display(Name = "تكلفة مباشرة")]
        Direct = 1,
        
        [Display(Name = "تكلفة غير مباشرة")]
        Indirect = 2,
        
        [Display(Name = "تكلفة ثابتة")]
        Fixed = 3,
        
        [Display(Name = "تكلفة متغيرة")]
        Variable = 4
    }

    /// <summary>
    /// حالة الدفع
    /// Payment status
    /// </summary>
    public enum PaymentStatus
    {
        [Display(Name = "معلق")]
        Pending = 1,
        
        [Display(Name = "مدفوع")]
        Paid = 2,
        
        [Display(Name = "متأخر")]
        Overdue = 3,
        
        [Display(Name = "ملغي")]
        Cancelled = 4
    }

    /// <summary>
    /// حالة التغذية
    /// Feeding status
    /// </summary>
    public enum FeedingStatus
    {
        [Display(Name = "مكتمل")]
        Completed = 1,
        
        [Display(Name = "جزئي")]
        Partial = 2,
        
        [Display(Name = "ملغي")]
        Cancelled = 3,
        
        [Display(Name = "متأخر")]
        Delayed = 4
    }

    /// <summary>
    /// حالة صحة الأسماك
    /// Fish health status
    /// </summary>
    public enum FishHealthStatus
    {
        [Display(Name = "صحي")]
        Healthy = 1,
        
        [Display(Name = "مريض")]
        Sick = 2,
        
        [Display(Name = "تحت العلاج")]
        UnderTreatment = 3,
        
        [Display(Name = "متعاف")]
        Recovered = 4
    }

    /// <summary>
    /// حالة النفوق
    /// Mortality status
    /// </summary>
    public enum MortalityStatus
    {
        [Display(Name = "مسجل")]
        Recorded = 1,
        
        [Display(Name = "قيد التحليل")]
        UnderAnalysis = 2,
        
        [Display(Name = "محلل")]
        Analyzed = 3,
        
        [Display(Name = "مغلق")]
        Closed = 4
    }

    /// <summary>
    /// حالة اختبار الجودة
    /// Quality test status
    /// </summary>
    public enum QualityTestStatus
    {
        [Display(Name = "ناجح")]
        Passed = 1,
        
        [Display(Name = "فاشل")]
        Failed = 2,
        
        [Display(Name = "معلق")]
        Pending = 3,
        
        [Display(Name = "قيد المراجعة")]
        UnderReview = 4
    }

    /// <summary>
    /// نظام التهوية
    /// Aeration system
    /// </summary>
    public enum AerationSystem
    {
        [Display(Name = "ميكانيكي")]
        Mechanical = 1,
        
        [Display(Name = "طبيعي")]
        Natural = 2,
        
        [Display(Name = "مختلط")]
        Hybrid = 3,
        
        [Display(Name = "غير متوفر")]
        NotAvailable = 4
    }

    /// <summary>
    /// نظام التصفية
    /// Filtration system
    /// </summary>
    public enum FiltrationSystem
    {
        [Display(Name = "بيولوجي")]
        Biological = 1,
        
        [Display(Name = "ميكانيكي")]
        Mechanical = 2,
        
        [Display(Name = "كيميائي")]
        Chemical = 3,
        
        [Display(Name = "مختلط")]
        Hybrid = 4
    }

    /// <summary>
    /// نظام التسخين
    /// Heating system
    /// </summary>
    public enum HeatingSystem
    {
        [Display(Name = "كهربائي")]
        Electric = 1,
        
        [Display(Name = "غاز")]
        Gas = 2,
        
        [Display(Name = "شمسي")]
        Solar = 3,
        
        [Display(Name = "غير متوفر")]
        NotAvailable = 4
    }

    /// <summary>
    /// نظام التبريد
    /// Cooling system
    /// </summary>
    public enum CoolingSystem
    {
        [Display(Name = "كهربائي")]
        Electric = 1,
        
        [Display(Name = "طبيعي")]
        Natural = 2,
        
        [Display(Name = "مختلط")]
        Hybrid = 3,
        
        [Display(Name = "غير متوفر")]
        NotAvailable = 4
    }

    /// <summary>
    /// نوع الشهادة
    /// Certification type
    /// </summary>
    public enum CertificationType
    {
        [Display(Name = "شهادة جودة")]
        Quality = 1,
        
        [Display(Name = "شهادة بيئية")]
        Environmental = 2,
        
        [Display(Name = "شهادة أمان")]
        Safety = 3,
        
        [Display(Name = "شهادة صحية")]
        Health = 4,
        
        [Display(Name = "شهادة أخرى")]
        Other = 5
    }

    /// <summary>
    /// حالة الشهادة
    /// Certification status
    /// </summary>
    public enum CertificationStatus
    {
        [Display(Name = "نشط")]
        Active = 1,
        
        [Display(Name = "منتهي الصلاحية")]
        Expired = 2,
        
        [Display(Name = "قيد التجديد")]
        Renewal = 3,
        
        [Display(Name = "ملغي")]
        Cancelled = 4
    }

    /// <summary>
    /// حالة العميل
    /// Customer status
    /// </summary>
    public enum CustomerStatus
    {
        [Display(Name = "نشط")]
        Active = 1,
        
        [Display(Name = "غير نشط")]
        Inactive = 2,
        
        [Display(Name = "معلق")]
        Suspended = 3,
        
        [Display(Name = "محظور")]
        Blocked = 4
    }

    /// <summary>
    /// نوع العميل
    /// Customer type
    /// </summary>
    public enum CustomerType
    {
        [Display(Name = "عميل فردي")]
        Individual = 1,
        
        [Display(Name = "عميل تجاري")]
        Business = 2,
        
        [Display(Name = "عميل حكومي")]
        Government = 3,
        
        [Display(Name = "عميل مؤسسي")]
        Institutional = 4,
        
        [Display(Name = "جملة")]
        Wholesale = 5,
        
        [Display(Name = "مطعم")]
        Restaurant = 6,
        
        [Display(Name = "سوبر ماركت")]
        Supermarket = 7,
        
        [Display(Name = "فندق")]
        Hotel = 8,
        
        [Display(Name = "مصدر")]
        Exporter = 9
    }

    /// <summary>
    /// حالة المورد
    /// Supplier status
    /// </summary>
    public enum SupplierStatus
    {
        [Display(Name = "نشط")]
        Active = 1,
        
        [Display(Name = "غير نشط")]
        Inactive = 2,
        
        [Display(Name = "معلق")]
        Suspended = 3,
        
        [Display(Name = "محظور")]
        Blocked = 4
    }

    /// <summary>
    /// نوع المورد
    /// Supplier type
    /// </summary>
    public enum SupplierType
    {
        [Display(Name = "مورد أعلاف")]
        Feed = 1,
        
        [Display(Name = "مورد معدات")]
        Equipment = 2,
        
        [Display(Name = "مورد أدوية")]
        Medicine = 3,
        
        [Display(Name = "مورد خدمات")]
        Service = 4,
        
        [Display(Name = "مورد عام")]
        General = 5,
        
        [Display(Name = "مورد أدوية بيطرية")]
        Medication = 6
    }

    /// <summary>
    /// نتيجة التدقيق
    /// Audit result
    /// </summary>
    public enum AuditResult
    {
        [Display(Name = "ناجح")]
        Passed = 1,
        
        [Display(Name = "ناجح مع ملاحظات")]
        PassedWithConditions = 2,
        
        [Display(Name = "راسب")]
        Failed = 3,
        
        [Display(Name = "بحاجة لتحسين")]
        NeedsImprovement = 4,
        
        [Display(Name = "في انتظار النتائج")]
        Pending = 5
    }

    /// <summary>
    /// خطورة الخطر
    /// Hazard severity
    /// </summary>
    public enum HazardSeverity
    {
        [Display(Name = "منخفض")]
        Low = 1,
        
        [Display(Name = "متوسط")]
        Medium = 2,
        
        [Display(Name = "عالي")]
        High = 3,
        
        [Display(Name = "حرج")]
        Critical = 4
    }

    /// <summary>
    /// احتمالية الخطر
    /// Hazard likelihood
    /// </summary>
    public enum HazardLikelihood
    {
        [Display(Name = "نادر")]
        Rare = 1,
        
        [Display(Name = "غير محتمل")]
        Unlikely = 2,
        
        [Display(Name = "محتمل")]
        Possible = 3,
        
        [Display(Name = "محتمل جداً")]
        Likely = 4,
        
        [Display(Name = "مؤكد")]
        Certain = 5
    }

    /// <summary>
    /// تكرار المراقبة
    /// Monitoring frequency
    /// </summary>
    public enum MonitoringFrequency
    {
        [Display(Name = "يومي")]
        Daily = 1,
        
        [Display(Name = "أسبوعي")]
        Weekly = 2,
        
        [Display(Name = "شهري")]
        Monthly = 3,
        
        [Display(Name = "ربعي")]
        Quarterly = 4,
        
        [Display(Name = "سنوي")]
        Annually = 5
    }

    /// <summary>
    /// نوع فحص الصحة
    /// Health inspection type
    /// </summary>
    public enum HealthInspectionType
    {
        [Display(Name = "فحص روتيني")]
        Routine = 1,
        
        [Display(Name = "فحص دوري")]
        Periodic = 2,
        
        [Display(Name = "فحص طارئ")]
        Emergency = 3,
        
        [Display(Name = "فحص ما قبل الحصاد")]
        PreHarvest = 4,
        
        [Display(Name = "فحص ما بعد الحصاد")]
        PostHarvest = 5
    }

    /// <summary>
    /// حالة الصحة
    /// Health status
    /// </summary>
    public enum HealthStatus
    {
        [Display(Name = "ممتاز")]
        Excellent = 1,
        
        [Display(Name = "جيد")]
        Good = 2,
        
        [Display(Name = "مقبول")]
        Fair = 3,
        
        [Display(Name = "ضعيف")]
        Poor = 4,
        
        [Display(Name = "حرج")]
        Critical = 5
    }

    /// <summary>
    /// حالة الامتثال
    /// Compliance status
    /// </summary>
    public enum ComplianceStatus
    {
        [Display(Name = "متوافق")]
        Compliant = 1,
        
        [Display(Name = "غير متوافق")]
        NonCompliant = 2,
        
        [Display(Name = "قيد المراجعة")]
        UnderReview = 3,
        
        [Display(Name = "معلق")]
        Pending = 4,
        
        [Display(Name = "مصحح")]
        Corrected = 5
    }

    /// <summary>
    /// حالة طلب البيع
    /// Sales order status
    /// </summary>
    public enum SalesOrderStatus
    {
        [Display(Name = "معلق")]
        Pending = 1,
        
        [Display(Name = "موافق")]
        Approved = 2,
        
        [Display(Name = "قيد التنفيذ")]
        InProgress = 3,
        
        [Display(Name = "مكتمل")]
        Completed = 4,
        
        [Display(Name = "ملغي")]
        Cancelled = 5
    }

    /// <summary>
    /// نوع عينة الجودة
    /// Quality sample type
    /// </summary>
    public enum QualitySampleType
    {
        [Display(Name = "عينة عشوائية")]
        Random = 1,
        
        [Display(Name = "عينة منتظمة")]
        Regular = 2,
        
        [Display(Name = "عينة خاصة")]
        Special = 3,
        
        [Display(Name = "عينة طوارئ")]
        Emergency = 4
    }

    /// <summary>
    /// نتيجة اختبار الجودة
    /// Quality test result
    /// </summary>
    public enum QualityTestResult
    {
        [Display(Name = "ناجح")]
        Passed = 0,
        
        [Display(Name = "ممتاز")]
        Excellent = 1,
        
        [Display(Name = "جيد")]
        Good = 2,
        
        [Display(Name = "مقبول")]
        Acceptable = 3,
        
        [Display(Name = "مشروط")]
        Conditional = 4,
        
        [Display(Name = "معلق")]
        Pending = 5,
        
        [Display(Name = "فاشل")]
        Failed = 6,
        
        [Display(Name = "ضعيف")]
        Poor = 7,
        
        [Display(Name = "مرفوض")]
        Rejected = 8
    }

    /// <summary>
    /// نقطة تحكم HACCP
    /// HACCP control point
    /// </summary>
    public enum HACCPControlPoint
    {
        [Display(Name = "استلام المواد الخام")]
        RawMaterialReceipt = 1,
        
        [Display(Name = "التخزين")]
        Storage = 2,
        
        [Display(Name = "التحضير")]
        Preparation = 3,
        
        [Display(Name = "المعالجة")]
        Processing = 4,
        
        [Display(Name = "التعبئة")]
        Packaging = 5,
        
        [Display(Name = "التوزيع")]
        Distribution = 6
    }

    /// <summary>
    /// نوع الخطر
    /// Hazard type
    /// </summary>
    public enum HazardType
    {
        [Display(Name = "بيولوجي")]
        Biological = 1,
        
        [Display(Name = "كيميائي")]
        Chemical = 2,
        
        [Display(Name = "فيزيائي")]
        Physical = 3,
        
        [Display(Name = "ميكروبي")]
        Microbiological = 4
    }

    public enum FoodSafetyControlMeasureType
    {
        [Display(Name = "برنامج متطلبات أساسية PRP")]
        Prp = 1,

        [Display(Name = "برنامج متطلبات تشغيلية OPRP")]
        Oprp = 2,

        [Display(Name = "نقطة تحكم حرجة CCP")]
        Ccp = 3
    }

    public enum HaccpRecordLifecycleStatus
    {
        [Display(Name = "مفتوح")]
        Open = 1,

        [Display(Name = "إجراء تصحيحي جارٍ")]
        CorrectiveActionInProgress = 2,

        [Display(Name = "بانتظار التحقق")]
        AwaitingVerification = 3,

        [Display(Name = "مغلق")]
        Closed = 4
    }

    public enum QualityRiskType
    {
        [Display(Name = "خطر")]
        Risk = 1,

        [Display(Name = "فرصة")]
        Opportunity = 2
    }

    public enum QualityRiskStatus
    {
        [Display(Name = "مفتوح")]
        Open = 1,

        [Display(Name = "قيد المعالجة")]
        InTreatment = 2,

        [Display(Name = "قيد المراقبة")]
        Monitoring = 3,

        [Display(Name = "مغلق")]
        Closed = 4
    }

    public enum QualityObjectiveStatus
    {
        [Display(Name = "نشط")]
        Active = 1,

        [Display(Name = "متحقق")]
        Achieved = 2,

        [Display(Name = "متأخر")]
        Overdue = 3,

        [Display(Name = "ملغى")]
        Cancelled = 4
    }

    public enum ControlledDocumentStatus
    {
        [Display(Name = "مسودة")]
        Draft = 1,

        [Display(Name = "معتمد وساري")]
        Approved = 2,

        [Display(Name = "ملغى / مؤرشف")]
        Obsolete = 3
    }

    public enum CapaStatus
    {
        [Display(Name = "مفتوح")]
        Open = 1,
        [Display(Name = "قيد التنفيذ")]
        InProgress = 2,
        [Display(Name = "بانتظار تحقق")]
        AwaitingVerification = 3,
        [Display(Name = "مغلق")]
        Closed = 4
    }

}
