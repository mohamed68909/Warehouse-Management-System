# WMS Pro - Agile Development Prompts (ASP.NET Core, DDD, MVC, REST API)

هذا الملف يحتوي على تقسيم مشروع **Warehouse Management System (WMS Pro)** بنظام **Agile** إلى **Sprints** (مراحل). لكل مهمة (Task) قمت بكتابة **"Prompt" (موجّه)** جاهز يمكنك استخدامه (أو إعطائه للذكاء الاصطناعي) لتوليد الكود خطوة بخطوة مع الالتزام بـ (DDD, Clean Architecture, Result Pattern, REST API, MVC).

---

## Sprint 1: Project Setup & Core Architecture (الأساسيات والهيكلة)

**الهدف:** بناء هيكل المشروع (Clean Architecture) وتجهيز الـ Result Pattern لتنظيم الردود.

### Prompt 1: Solution Setup & Clean Architecture Layers
> "أريد إنشاء مشروع ASP.NET Core جديد بنظام Clean Architecture وتطبيق Domain-Driven Design (DDD). 
> قم بإنشاء Solution باسم `WMSPro` يحتوي على المشاريع التالية:
> 1. `WMSPro.Domain` (Class Library)
> 2. `WMSPro.Application` (Class Library)
> 3. `WMSPro.Infrastructure` (Class Library)
> 4. `WMSPro.Api` (ASP.NET Core Web API)
> 5. `WMSPro.Mvc` (ASP.NET Core MVC لعرض الـ UI).
> أضف الـ References الصحيحة بين المشاريع (Api و Mvc يعتمدان على Application و Infrastructure. و Application يعتمد على Domain. و Infrastructure يعتمد على Application). يرجى إعطائي أوامر .NET CLI لإنشاء هذا الهيكل."

### Prompt 2: The Result Pattern Implementation
> "في مشروع `WMSPro.Domain`، أريد تطبيق الـ **Result Pattern** للتعامل مع الردود والأخطاء بدلاً من رمي الـ Exceptions.
> قم بكتابة كلاس `Result` وكلاس `Result<T>` و `Error` record. 
> يجب أن يحتوي الـ `Result` على خصائص مثل `IsSuccess` و `IsFailure` و `Error`. 
> أضف أيضاً كلاس `DomainException` للتعامل مع أخطاء البزنس في حال لزم الأمر."

---

## Sprint 2: Authentication & User Management (تسجيل الدخول والصلاحيات)

**الهدف:** إعداد نظام الـ Auth مع الأدوار (Manager, Employee) ليكون جاهزاً للتوسع المستقبلي.

### Prompt 3: Identity & User Entity
> "في مشروع `WMSPro.Domain`، قم بإنشاء الـ Entity الخاص بالمستخدم `User`.
> في مشروع `WMSPro.Infrastructure`، قم بإعداد **ASP.NET Core Identity**، ولكن اجعله يتماشى مع الـ Clean Architecture.
> قم بإنشاء كلاس `ApplicationDbContext` يرث من `IdentityDbContext`.
> أضف الـ Roles الأساسية (Manager, Warehouse Employee) كـ Seed Data عند تشغيل قاعدة البيانات (EF Core Migrations)."

### Prompt 4: Auth API & JWT
> "في مشروع `WMSPro.Application`، أنشئ `IAuthService` و DTOs لـ (Login, Register).
> قم بتنفيذ هذه الـ Interface في الـ `Infrastructure` لتوليد **JWT Token**.
> تأكد من أن الـ Service تُرجع `Result<AuthResponse>` باستخدام الـ Result Pattern الذي أنشأناه سابقاً.
> في `WMSPro.Api`، قم بإنشاء `AuthController` يحتوي على Endpoints لـ Login و Register."

### Prompt 5: MVC Auth UI
> "في مشروع `WMSPro.Mvc`، أريد إنشاء صفحات (Views) لتسجيل الدخول وإنشاء حساب (بناءً على تصميم WMS Pro).
> قم بإنشاء `AccountController`. اجعل هذا الـ Controller يستهلك الـ `AuthController` الموجود في الـ REST API باستخدام `HttpClient`، ويقوم بتخزين الـ JWT Token في الـ Cookies لكي يظل المستخدم مسجلاً للدخول."

---

## Sprint 3: Core Domain (المنتجات والمخازن)

**الهدف:** بناء الـ Entities الأساسية والـ CRUD Operations الخاصة بها.

### Prompt 6: Warehouse Domain & CQRS
> "في `WMSPro.Domain`، أنشئ الـ Entity `Warehouse` الذي يحتوي على (Id, Name, Capacity, Location, ManagerName, Phone, Email).
> تأكد من تطبيق مبادئ DDD (Rich Domain Model، إخفاء الـ Setters، واستخدام Factory Method أو Constructor لإنشاء الـ Entity).
> في `WMSPro.Application`، باستخدام **MediatR**، قم بإنشاء الـ Commands والـ Queries الخاصة بالمخزن (CreateWarehouseCommand, GetWarehousesQuery، إلخ)، واجعل الـ Handlers تُرجع `Result`."

### Prompt 7: Product Domain & CQRS
> "بشكل مشابه للمخازن، قم بإنشاء الـ Entity `Product` في الـ `Domain` بخصائص (Id, Name, SKU, Category, Price, Quantity).
> أنشئ الـ Commands والـ Queries الخاصة بالمنتج باستخدام **MediatR** في الـ `Application` layer، وتأكد من استخدام الـ Result Pattern."

### Prompt 8: API & MVC Views for Warehouses and Products
> "في `WMSPro.Api`، أنشئ `WarehousesController` و `ProductsController` كـ RESTful APIs.
> في `WMSPro.Mvc`، قم بإنشاء الـ Views لعرض قائمة المخازن (Warehouses List) وقائمة المنتجات (Products List) وإضافة/تعديل باستخدام Modals (باستخدام Bootstrap أو Vanilla CSS/JS للـ Modals). 
> اجعل الـ MVC Controller يتصل بالـ API لجلب وإرسال البيانات."

---

## Sprint 4: Inventory & Transactions (حركة المخزون)

**الهدف:** إدارة حركة المنتجات داخل المخازن (إدخال، إخراج، نقل).

### Prompt 9: Transaction Entity & Domain Logic
> "في `WMSPro.Domain`، أنشئ Entity باسم `InventoryTransaction`.
> يجب أن يحتوي على (Type: IN, OUT, TRANSFER)، ProductId، SourceWarehouseId (Nullable)، DestinationWarehouseId (Nullable)، Quantity، Reference، و Date.
> أضف Domain Logic (Business Rule) يمنع عمل 'Stock OUT' أو 'Transfer' إذا كانت الكمية المطلوبة غير متوفرة في المخزن (ارمي Domain Error)."

### Prompt 10: Transaction Application Logic (MediatR)
> "في `WMSPro.Application`، أنشئ `CreateTransactionCommand` والذي سيقوم بالتالي:
> 1. إنشاء سجل الـ Transaction.
> 2. تحديث كمية المنتج `Quantity` في المخزن المعني (خصم في حالة OUT، زيادة في حالة IN، وتعديل الاثنين في حالة TRANSFER).
> يجب أن يتم كل هذا داخل Database Transaction واحدة (Unit of Work) لضمان صحة البيانات."

### Prompt 11: Transactions UI & Inventory View
> "أنشئ Endpoints في الـ API لجلب حركات المخزون.
> في الـ MVC، أنشئ View لصفحة `Transactions` تحتوي على Data Table لعرض الحركات مع إمكانية الفلترة، وصفحة `Inventory` لعرض الكميات الحالية لكل منتج في كل مخزن."

---

## Sprint 5: Orders Management (إدارة الطلبات)

**الهدف:** التعامل مع طلبات البيع والشراء وتأثيرها على المخزون.

### Prompt 12: Order & OrderLine Entities
> "في `WMSPro.Domain`، قم بإنشاء الـ Aggregate Root `Order` (يحتوي على OrderNumber, Type [Purchase, Sales], SupplierCustomerName, Status, TotalAmount, Date).
> و Entity `OrderItem` كجزء من الـ Order.
> تطبيق DDD: الـ OrderItem يجب أن يضاف فقط من خلال ميثود داخل الـ Order `AddOrderItem()`.
> عند تغيير حالة الطلب إلى Completed، يجب إطلاق **Domain Event** (`OrderCompletedEvent`)."

### Prompt 13: Order Domain Events & MediatR
> "في `WMSPro.Application`، قم بإنشاء Event Handler للـ `OrderCompletedEvent`.
> عندما يتم اكتمال طلب الشراء (Purchase Order)، يجب إنشاء `InventoryTransaction` من نوع (IN) تلقائياً.
> وعند اكتمال طلب البيع (Sales Order)، يجب إنشاء `InventoryTransaction` من نوع (OUT) تلقائياً."

---

## Sprint 6: Dashboard & Analytics (لوحة التحكم)

**الهدف:** تجميع البيانات وعرضها في لوحة تحكم تفاعلية.

### Prompt 14: Dashboard Dashboard Stats Query
> "في `WMSPro.Application`، قم بإنشاء `GetDashboardStatsQuery`.
> هذا الـ Query يجب أن يُرجع DTO يحتوي على:
> TotalProducts, TotalStock, LowStockAlertsCount, TotalWarehouses.
> وقائمة من `StockMovementTrendDto` لرسم الـ Line Chart.
> نفذ هذا في الـ API Controller."

### Prompt 15: Dashboard MVC View
> "في `WMSPro.Mvc`، قم ببناء صفحة `Dashboard`.
> استدعِ بيانات الإحصائيات من الـ API. 
> استخدم مكتبة مثل `Chart.js` لرسم الـ Stock Movement Trend والـ Stock IN vs OUT.
> اعرض بطاقات الـ KPIs وتأكد من مطابقة التصميم لشاشات WMS Pro."

---

### نصائح عامة لك أثناء العمل:
*   **Dependency Injection:** تأكد من إعداد الـ DI لكل Sprint في ملف `Program.cs`.
*   **Unit of Work & Repository Pattern:** يفضل تطبيقهما في الـ `Infrastructure` مع الـ `Domain` Interfaces لضمان مبدأ Clean Architecture.
*   **Validation:** استخدم `FluentValidation` في طبقة الـ Application للتحقق من صحة البيانات قبل إرسالها للـ Commands.
*   **API vs MVC:** بما أنك تستخدم MVC لعرض الـ UI، قمت بتوجيهك بحيث يقوم الـ MVC باستهلاك الـ REST API كعميل (Client) عبر الـ `HttpClient`. هذا يفصل الباك إند عن الـ UI تماماً ويجعل الـ API جاهزاً إذا أردت عمل تطبيق موبايل لاحقاً للموظفين في المخزن.
