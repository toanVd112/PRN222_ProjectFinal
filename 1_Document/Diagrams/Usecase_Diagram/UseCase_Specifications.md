# Use Case Specifications
**Project:** Classroom Equipment Management System (CEMS)  
**Version:** 1.1 — Cập nhật theo source code thực tế (ASP.NET MVC)  
**Role:** Business Analyst / System Architect

Tài liệu này đặc tả chi tiết **toàn bộ** các Use Case của hệ thống CEMS, đối chiếu trực tiếp với Controller actions đã được implement.

---

## PHẦN 1: USE CASE CHUNG (SHARED — ALL ROLES)

### 1. UC_LogIn (Đăng nhập)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Log In |
| **2. Actor(s):** | Admin, Technician, Lecturer |
| **3. Summary Description:** | Cho phép người dùng xác thực và đăng nhập vào hệ thống theo quyền hạn tương ứng. |
| **4. Priority:** | High |
| **5. Status:** | Implemented |
| **6. Pre-Condition:** | Người dùng có tài khoản hợp lệ, tài khoản không bị khóa. |
| **7. Post-Condition(s):** | Phiên làm việc (Session/Cookie) được khởi tạo với phân quyền đúng vai trò. Người dùng được điều hướng đến Dashboard tương ứng với Role. |
| **8. Basic Path:** | 1. Người dùng nhập Username và Password.<br>2. Nhấn "Đăng nhập".<br>3. Hệ thống đối chiếu DB (ASP.NET Identity / bcrypt).<br>4. Hệ thống cấp Cookie Authentication và điều hướng đến `HomeController.Index()` — Dashboard theo Role. |
| **9. Alternative Paths:** | - **3a. Sai thông tin:** Hệ thống hiển thị lỗi "Sai tài khoản hoặc mật khẩu".<br>- **3b. Tài khoản bị khóa (IsActive = false):** Hệ thống hiển thị thông báo "Tài khoản của bạn đã bị khóa, vui lòng liên hệ Admin". |
| **10. Business Rules:** | Tài khoản bị khóa (`IsActive = false`) không thể đăng nhập. Mỗi Role thấy đúng menu và chức năng tương ứng sau đăng nhập. |

### 2. UC_SendNotification (Hệ thống gửi thông báo)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Send Notification |
| **2. Actor(s):** | System |
| **3. Summary Description:** | Chức năng nội bộ tự động tạo và lưu in-app notification vào bảng `Notifications` khi có sự kiện nghiệp vụ xảy ra. |
| **4. Priority:** | High |
| **5. Status:** | Implemented |
| **6. Pre-Condition:** | Nhận được lệnh kích hoạt (Trigger) từ một Use Case khác (gửi báo cáo, phân công, giải quyết sự cố...). |
| **7. Post-Condition(s):** | Bản ghi Notification được tạo trong DB. Người nhận thấy chuông thông báo khi truy cập hệ thống. |
| **8. Basic Path:** | 1. Use Case cha gọi service tạo notification.<br>2. Hệ thống tạo record `Notification` (UserId người nhận, nội dung, loại, link liên kết).<br>3. Lưu vào DB.<br>4. Badge số trên thanh navbar được cập nhật realtime khi người nhận reload trang. |
| **9. Alternative Paths:** | Không có — đây là fire-and-forget, không block Use Case cha. |
| **10. Business Rules:** | Nội dung thông báo thay đổi động theo loại sự kiện. Notification chưa đọc hiển thị badge đỏ trên chuông. |

### 3. UC_ManageProfile (Quản lý hồ sơ cá nhân)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Manage Personal Profile |
| **2. Actor(s):** | Admin, Technician, Lecturer |
| **3. Summary Description:** | Cho phép người dùng xem và cập nhật thông tin cá nhân, điều hướng đến chức năng đổi mật khẩu. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`ProfileController.Index`, `ProfileController.Update`) |
| **6. Pre-Condition:** | Người dùng đã đăng nhập thành công vào hệ thống. |
| **7. Post-Condition(s):** | Thông tin cá nhân được cập nhật thành công vào DB. |
| **8. Basic Path:** | 1. Người dùng chọn "Hồ sơ cá nhân" từ thanh điều hướng.<br>2. Hệ thống lấy thông tin tài khoản hiện tại từ DB.<br>3. Hiển thị: Mã số, Họ tên, Email, Vai trò, Số điện thoại.<br>4. Người dùng chỉnh sửa thông tin được phép (Họ tên, SĐT).<br>5. Nhấn "Lưu thay đổi".<br>6. Hệ thống xác thực và cập nhật DB. |
| **9. Alternative Paths:** | - **4a. Đổi mật khẩu:** Kích hoạt `<<extend>>` UC_ChangePassword.<br>- **5a. Hủy:** Quay lại trang chủ, không lưu. |
| **10. Business Rules:** | Các trường Email và Vai trò ở chế độ chỉ đọc (Read-only). |

### 4. UC_ChangePassword (Đổi mật khẩu)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Change Password |
| **2. Actor(s):** | Admin, Technician, Lecturer |
| **3. Summary Description:** | Cho phép người dùng đã đăng nhập tự thay đổi mật khẩu của mình. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`ProfileController.ChangePassword`) |
| **6. Pre-Condition:** | Người dùng đang ở màn hình Hồ sơ cá nhân (Extend UC_ManageProfile). |
| **7. Post-Condition(s):** | Mật khẩu mới được hash (bcrypt) và lưu vào DB. |
| **8. Basic Path:** | 1. Nhấn "Đổi mật khẩu" trên trang Profile.<br>2. Nhập mật khẩu hiện tại, mật khẩu mới và xác nhận mật khẩu mới.<br>3. Nhấn "Cập nhật".<br>4. Hệ thống kiểm tra mật khẩu hiện tại khớp.<br>5. Mã hóa mật khẩu mới (BCrypt) và cập nhật DB. |
| **9. Alternative Paths:** | - **4a. Sai mật khẩu cũ:** Báo lỗi "Mật khẩu hiện tại không đúng".<br>- **4b. Xác nhận không khớp:** Báo lỗi validation. |
| **10. Business Rules:** | Mật khẩu mới phải đáp ứng độ phức tạp tối thiểu. |

### 5. UC_ForgotPassword (Quên mật khẩu)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Forgot Password |
| **2. Actor(s):** | Admin, Technician, Lecturer |
| **3. Summary Description:** | Cho phép người dùng đặt lại mật khẩu từ màn hình đăng nhập qua email. |
| **4. Priority:** | High |
| **5. Status:** | Implemented |
| **6. Pre-Condition:** | Người dùng chưa đăng nhập và đang ở màn hình đăng nhập. |
| **7. Post-Condition(s):** | Email chứa link đặt lại mật khẩu được gửi đến người dùng. |
| **8. Basic Path:** | 1. Bấm "Quên mật khẩu" trên màn hình đăng nhập.<br>2. Nhập email tài khoản.<br>3. Hệ thống kiểm tra email tồn tại trong DB.<br>4. Tạo reset token.<br>5. Hệ thống gọi `<<include>> Send Notification` gửi email chứa link đặt lại. |
| **9. Alternative Paths:** | - **3a. Email không tồn tại:** Hệ thống hiển thị lỗi. |
| **10. Business Rules:** | Reset token có thời hạn sử dụng giới hạn. |

### 6. UC_ViewDashboard (Xem Dashboard)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | View Dashboard |
| **2. Actor(s):** | Admin, Technician, Lecturer |
| **3. Summary Description:** | Trang tổng quan hiển thị số liệu thống kê và hoạt động gần đây **khác nhau theo từng Role**. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`HomeController.Index()` — render khác nhau theo Role) |
| **6. Pre-Condition:** | Người dùng đã đăng nhập thành công. |
| **7. Post-Condition(s):** | Dashboard hiển thị số liệu real-time từ DB theo Role. |
| **8. Basic Path:** | 1. Người dùng truy cập trang chủ.<br>2. `HomeController.Index()` detect Role từ Claims.<br>3. Truy vấn dữ liệu tương ứng Role từ DB.<br>4. Render giao diện Dashboard đúng Role. |
| **9. Alternative Paths:** | - **Không có dữ liệu:** Hiển thị thông báo tương ứng từng section. |
| **10. Business Rules:** | **Admin Dashboard:** Tổng thiết bị, sự cố mới, yêu cầu thanh lý/chuyển đang chờ duyệt, lịch sử hoạt động gần đây.<br>**Technician Dashboard:** Số sự cố được giao (đang xử lý, đã giải quyết), số lần luân chuyển, sự cố cần xử lý ngay, lối tắt nhanh.<br>**Lecturer Dashboard:** Số phòng đang dạy, số báo cáo đã gửi (đang chờ/đã giải quyết), lịch sử báo cáo gần đây. |

### 7. UC_ViewNotifications (Xem thông báo)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | View Notifications |
| **2. Actor(s):** | Admin, Technician, Lecturer |
| **3. Summary Description:** | Người dùng xem danh sách thông báo và đánh dấu đã đọc từ icon chuông trên navbar. |
| **4. Priority:** | Medium |
| **5. Status:** | Implemented (`NotificationsController.Read()`) |
| **6. Pre-Condition:** | Người dùng đã đăng nhập. |
| **7. Post-Condition(s):** | Thông báo được đánh dấu là đã đọc (`IsRead = true`). |
| **8. Basic Path:** | 1. Nhấn vào icon chuông trên navbar.<br>2. Dropdown hiện danh sách thông báo chưa đọc.<br>3. Nhấn vào thông báo → hệ thống gọi `NotificationsController.Read(id)` đánh dấu đã đọc và chuyển hướng đến trang liên quan. |
| **9. Alternative Paths:** | - **Không có thông báo:** Hiển thị "Không có thông báo mới". |
| **10. Business Rules:** | Mỗi người chỉ thấy thông báo của mình (UserId). |

---

## PHẦN 2: ACTOR - ADMIN

### 8. UC_ManageUserAccount (Quản lý tài khoản)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Manage User Account |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Xem danh sách, tìm kiếm, lọc và thực hiện các thao tác quản trị trên tài khoản người dùng. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`UserController.Index()`) |
| **6. Pre-Condition:** | Admin đã đăng nhập thành công. |
| **7. Post-Condition(s):** | Danh sách người dùng được hiển thị. |
| **8. Basic Path:** | 1. Admin truy cập "Quản lý tài khoản" từ menu.<br>2. Hệ thống truy vấn danh sách người dùng (có phân trang, lọc theo Role/Status/tìm kiếm).<br>3. Hiển thị dạng bảng (Mã, Họ tên, Email, Vai trò, Trạng thái). |
| **9. Alternative Paths:** | - **3a. Tìm kiếm/Lọc:** Nhập từ khóa hoặc chọn bộ lọc theo vai trò/trạng thái.<br>- **3b. Tạo tài khoản Giảng viên:** `<<extend>>` UC_CreateLecturerAccount.<br>- **3c. Tạo tài khoản Technician:** `<<extend>>` UC_CreateTechnicianAccount.<br>- **3d. Xem chi tiết:** `<<extend>>` UC_ViewUserAccount.<br>- **3e. Sửa tài khoản:** `<<extend>>` UC_UpdateUserAccount.<br>- **3f. Khóa/Mở khóa tài khoản:** `<<extend>>` UC_LockUnlockUserAccount. |
| **10. Business Rules:** | Dữ liệu phân trang, sắp xếp mặc định theo ngày tạo mới nhất. Admin không tự khóa tài khoản của chính mình. |

### 9. UC_CreateLecturerAccount (Tạo tài khoản Giảng viên)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Create Lecturer Account |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Tạo mới tài khoản cho Giảng viên. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`UserController.Create()`) |
| **6. Pre-Condition:** | Đang ở màn hình Quản lý tài khoản (Extend UC_ManageUserAccount). |
| **7. Post-Condition(s):** | Tài khoản Lecturer được tạo trong DB với Role = Lecturer. |
| **8. Basic Path:** | 1. Chọn "Tạo tài khoản" → Chọn Role = Lecturer.<br>2. Điền thông tin (Họ tên, Email, SĐT, Mật khẩu).<br>3. Nhấn "Tạo".<br>4. Hệ thống kiểm tra Email không trùng.<br>5. Hash mật khẩu (BCrypt), lưu DB với Role = Lecturer. |
| **9. Alternative Paths:** | - **4a. Email đã tồn tại:** Báo lỗi không cho tạo. |
| **10. Business Rules:** | Email phải là duy nhất. Role = Lecturer được gán cứng. |

### 10. UC_CreateTechnicianAccount (Tạo tài khoản Technician)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Create Technician Account |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Tạo mới tài khoản phân quyền Technician. |
| **4. Priority:** | Medium |
| **5. Status:** | Implemented (`UserController.Create()`) |
| **6. Pre-Condition:** | Đang ở màn hình Quản lý tài khoản (Extend UC_ManageUserAccount). |
| **7. Post-Condition(s):** | Tài khoản Technician được tạo trong DB với Role = Technician. |
| **8. Basic Path:** | Tương tự UC_CreateLecturerAccount nhưng Role = Technician. |
| **9. Alternative Paths:** | Tương tự UC_CreateLecturerAccount. |
| **10. Business Rules:** | Role = Technician. Email không được trùng lặp. |

### 11. UC_ViewUserAccount (Xem chi tiết tài khoản)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | View User Account |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Xem chi tiết thông tin của một tài khoản người dùng cụ thể. |
| **4. Priority:** | Medium |
| **5. Status:** | Implemented (`UserController.Details(id)`) |
| **6. Pre-Condition:** | Đang ở màn hình Quản lý tài khoản. |
| **7. Post-Condition(s):** | Chi tiết tài khoản được hiển thị ở chế độ Read-only. |
| **8. Basic Path:** | 1. Admin chọn tài khoản trong danh sách.<br>2. Nhấn nút "Xem chi tiết".<br>3. Hệ thống hiển thị thông tin chi tiết tài khoản. |
| **9. Alternative Paths:** | Không. |
| **10. Business Rules:** | Thông tin hiển thị ở chế độ chỉ đọc (Read-only). |

### 12. UC_UpdateUserAccount (Cập nhật thông tin tài khoản)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Update User Account |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Sửa thông tin cơ bản của tài khoản (Họ tên, SĐT, Role). |
| **4. Priority:** | Medium |
| **5. Status:** | Implemented (`UserController.Edit(id)`) |
| **6. Pre-Condition:** | Đang ở màn hình Quản lý tài khoản. |
| **7. Post-Condition(s):** | Thông tin tài khoản được cập nhật vào DB. |
| **8. Basic Path:** | 1. Chọn tài khoản → Bấm "Chỉnh sửa".<br>2. Thay đổi thông tin.<br>3. Nhấn "Lưu".<br>4. Hệ thống xác thực và cập nhật DB. |
| **9. Alternative Paths:** | - **4a. Trùng Email:** Báo lỗi không cho lưu. |
| **10. Business Rules:** | Admin không thể tự thay đổi Role của chính mình xuống cấp thấp hơn. |

### 13. UC_LockUnlockUserAccount (Khóa/Mở khóa tài khoản)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Lock/Unlock User Account |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Khóa hoặc mở khóa tài khoản người dùng (toggle `IsActive`). Không xóa vật lý (Soft Delete). |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`UserController.ToggleStatus(id)`) |
| **6. Pre-Condition:** | Đang ở màn hình Quản lý tài khoản. |
| **7. Post-Condition(s):** | Trạng thái `IsActive` của tài khoản được đổi ngược lại (true→false hoặc false→true). Tài khoản bị khóa không thể đăng nhập. |
| **8. Basic Path:** | 1. Admin chọn tài khoản cần thay đổi trạng thái.<br>2. Nhấn nút "Khóa" hoặc "Mở khóa".<br>3. Hệ thống cập nhật `IsActive = !IsActive` trong DB.<br>4. Hiển thị thông báo thành công. |
| **9. Alternative Paths:** | - **Tự khóa:** Admin không thể khóa tài khoản của chính mình. |
| **10. Business Rules:** | Hệ thống chỉ dùng Soft Delete (`IsActive = false`) để bảo toàn lịch sử dữ liệu. Không thực hiện hard delete. |

### 14. UC_ManageRoom (Quản lý phòng học)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Manage Room |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Thêm, sửa, kích hoạt/vô hiệu hóa thông tin phòng học. |
| **4. Priority:** | Medium |
| **5. Status:** | Implemented (`RoomController`: Index, Create, Edit, ToggleStatus) |
| **6. Pre-Condition:** | Admin đã đăng nhập thành công. |
| **7. Post-Condition(s):** | Dữ liệu phòng học được cập nhật. |
| **8. Basic Path:** | 1. Vào Quản lý phòng.<br>2. Thêm mới hoặc sửa thông tin phòng (Tên, Vị trí, Loại phòng, Sức chứa).<br>3. Lưu vào DB. |
| **9. Alternative Paths:** | - **Vô hiệu hóa phòng:** Sử dụng ToggleStatus, không xóa vật lý nếu còn thiết bị. |
| **10. Business Rules:** | Mã phòng là duy nhất. Phòng có thiết bị đang hoạt động không thể vô hiệu hóa. |

### 15. UC_ManageEquipmentCategory (Quản lý loại thiết bị)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Manage Equipment Category |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Quản lý danh mục loại thiết bị (Máy chiếu, Điều hòa, Máy tính...). |
| **4. Priority:** | Medium |
| **5. Status:** | Implemented (`EquipmentCategoryController`) |
| **6. Pre-Condition:** | Admin đã đăng nhập thành công. |
| **7. Post-Condition(s):** | Dữ liệu Category được cập nhật. |
| **8. Basic Path:** | 1. Vào Quản lý danh mục.<br>2. Thêm/sửa danh mục (Tên, Mô tả).<br>3. Lưu. |
| **9. Alternative Paths:** | - **Xóa danh mục:** Chặn nếu đang có thiết bị thuộc danh mục này. |
| **10. Business Rules:** | Tên danh mục không được để trống và không được trùng lặp. |

### 16. UC_ViewEquipmentList (Xem danh sách thiết bị)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | View Equipment List |
| **2. Actor(s):** | Admin, Technician |
| **3. Summary Description:** | Xem danh sách toàn bộ thiết bị trong hệ thống với bộ lọc theo phòng, loại, trạng thái. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`EquipmentsController.Index()` — `[Authorize(Roles="Technician,Admin")]`) |
| **6. Pre-Condition:** | Người dùng đã đăng nhập với Role Admin hoặc Technician. |
| **7. Post-Condition(s):** | Danh sách thiết bị được hiển thị theo bộ lọc đã chọn. |
| **8. Basic Path:** | 1. Truy cập menu Quản lý thiết bị.<br>2. Hệ thống hiển thị danh sách thiết bị (có phân trang).<br>3. Người dùng lọc theo Phòng, Loại thiết bị, Trạng thái, hoặc tìm kiếm theo tên/mã. |
| **9. Alternative Paths:** | - **Không có thiết bị:** Hiển thị "Chưa có thiết bị nào". |
| **10. Business Rules:** | Admin xem được toàn bộ. Technician có thêm nút Thêm/Sửa/Đề xuất thanh lý. |

### 17. UC_AssignIncident (Phân công sự cố cho Technician)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Assign Incident to Technician |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Admin chỉ định một Technician cụ thể để xử lý một sự cố đang chờ. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`TechnicianIncidentsController.Assign(id, technicianId)` — `[Authorize(Roles="Admin")]`) |
| **6. Pre-Condition:** | Có sự cố ở trạng thái Pending chưa được phân công. Admin đã đăng nhập. |
| **7. Post-Condition(s):** | Trường `AssignedTo` của IncidentReport được cập nhật. Technician nhận được in-app notification. |
| **8. Basic Path:** | 1. Admin vào danh sách sự cố (TechnicianIncidents/Index).<br>2. Chọn sự cố cần phân công.<br>3. Chọn Technician từ dropdown.<br>4. Nhấn "Phân công".<br>5. Hệ thống lưu `AssignedTo = technicianId`.<br>6. Hệ thống `<<include>>` UC_SendNotification thông báo cho Technician. |
| **9. Alternative Paths:** | - **Không có Technician:** Danh sách dropdown rỗng → Admin cần tạo tài khoản Technician trước. |
| **10. Business Rules:** | Chỉ Admin mới có quyền phân công. Một sự cố có thể được phân công lại. |

### 18. UC_ReviewTransferRequest (Duyệt yêu cầu luân chuyển)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Review Transfer Request |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Admin xem xét và phê duyệt hoặc từ chối yêu cầu luân chuyển thiết bị do Technician đề xuất. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`EquipmentsController.TransferRequests()`, `ReviewTransferRequest()` — `[Authorize(Roles="Admin")]`) |
| **6. Pre-Condition:** | Có yêu cầu luân chuyển ở trạng thái Pending. Admin đã đăng nhập. |
| **7. Post-Condition(s):** | Nếu duyệt: thiết bị được cập nhật vị trí mới, bản ghi `TransferHistory` được tạo. Technician nhận notification kết quả. |
| **8. Basic Path:** | 1. Vào danh sách yêu cầu luân chuyển.<br>2. Xem chi tiết yêu cầu (thiết bị nào, từ phòng nào, đến phòng/kho nào, lý do).<br>3. Bấm "Duyệt" hoặc "Từ chối".<br>4. Nhập ghi chú (nếu từ chối).<br>5. Hệ thống cập nhật DB và gửi notification cho Technician. |
| **9. Alternative Paths:** | - **Từ chối:** Admin nhập lý do → Yêu cầu bị hủy, thiết bị giữ nguyên vị trí cũ. |
| **10. Business Rules:** | Chỉ Admin được duyệt. Khi duyệt, hệ thống tự cập nhật `RoomId` của thiết bị và tạo `TransferHistory`. |

### 19. UC_ApproveEquipmentDisposal (Phê duyệt thanh lý)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Approve Equipment Disposal |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Phê duyệt hoặc từ chối yêu cầu thanh lý thiết bị hỏng do Technician đề xuất. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`EquipmentsController.Disposals()`, `ReviewDisposalRequest()` — `[Authorize(Roles="Admin")]`) |
| **6. Pre-Condition:** | Có yêu cầu thanh lý ở trạng thái Pending. |
| **7. Post-Condition(s):** | Nếu duyệt: thiết bị chuyển sang trạng thái "Disposed". Các sự cố liên quan được tự động đóng. |
| **8. Basic Path:** | 1. Mở danh sách chờ thanh lý.<br>2. Chọn thiết bị.<br>3. Bấm "Phê duyệt" hoặc "Từ chối".<br>4. Hệ thống cập nhật trạng thái thiết bị.<br>5. Nếu duyệt: tự động đóng tất cả IncidentReport liên quan. |
| **9. Alternative Paths:** | - **Từ chối:** Thiết bị trả về trạng thái trước đó ("Đang sửa chữa"). |
| **10. Business Rules:** | Thiết bị đã Disposed vẫn lưu trong DB nhưng không xuất hiện trong kho hoạt động. |

---

## PHẦN 3: ACTOR - TECHNICIAN

### 20. UC_AddUpdateEquipment (Thêm/Sửa thông tin thiết bị)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Add/Update Equipment |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Nhập kho thiết bị mới hoặc cập nhật thông tin (bảo hành, serial, trạng thái). |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`EquipmentsController.Create()`, `Edit()`) |
| **6. Pre-Condition:** | Technician đăng nhập thành công. |
| **7. Post-Condition(s):** | Dữ liệu thiết bị lưu thành công trong DB. |
| **8. Basic Path:** | 1. Chọn "Thêm thiết bị mới".<br>2. Nhập: Tên, Asset Code, Loại, Phòng ban đầu, Ngày mua, Số serial, Bảo hành.<br>3. Lưu. Hệ thống kiểm tra Asset Code duy nhất. |
| **9. Alternative Paths:** | - **Asset Code trùng:** Báo lỗi "Mã tài sản đã tồn tại". |
| **10. Business Rules:** | Asset Code phải duy nhất trong toàn hệ thống. Technician không có quyền xóa (hard delete) thiết bị. |

### 21. UC_ProposeTransferRequest (Đề xuất yêu cầu luân chuyển)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Propose Transfer Request |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Đề xuất luân chuyển thiết bị từ phòng hiện tại đến phòng học khác hoặc về kho lưu trữ (Storage). Yêu cầu Admin phê duyệt. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`EquipmentsController.ProposeTransfer()`) |
| **6. Pre-Condition:** | Thiết bị đang ở trạng thái Active (đang sử dụng). Technician đăng nhập. |
| **7. Post-Condition(s):** | Bản ghi `TransferRequest` được tạo với trạng thái Pending. Admin nhận notification để duyệt. |
| **8. Basic Path:** | 1. Chọn thiết bị cần luân chuyển.<br>2. Nhấn "Đề xuất luân chuyển".<br>3. Chọn điểm đến: Phòng học khác **hoặc** Kho lưu trữ (Storage — RoomId = null).<br>4. Nhập lý do luân chuyển.<br>5. Gửi đề xuất. Hệ thống tạo `TransferRequest` (Status = Pending). |
| **9. Alternative Paths:** | - **Phòng đích trùng phòng hiện tại:** Báo lỗi "Thiết bị đang ở phòng này".<br>- **Đã có đề xuất đang chờ duyệt:** Báo lỗi không cho tạo trùng. |
| **10. Business Rules:** | Một thiết bị chỉ có thể có một Transfer Request đang Pending tại một thời điểm. Mọi chuyển phòng đều phải qua quy trình đề xuất → Admin duyệt. Khi về kho: `RoomId = null`. |

### 22. UC_ProposeDisposal (Đề xuất thanh lý thiết bị)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Propose Equipment Disposal |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Đề xuất thanh lý thiết bị không thể sửa chữa được gửi lên Admin phê duyệt. |
| **4. Priority:** | Medium |
| **5. Status:** | Implemented (`EquipmentsController.ProposeDisposal()`) |
| **6. Pre-Condition:** | Thiết bị đang ở trạng thái "Đang sửa chữa" hoặc lỗi nghiêm trọng. |
| **7. Post-Condition(s):** | `DisposalRequest` được tạo trong DB với Status = Pending. |
| **8. Basic Path:** | 1. Xem chi tiết thiết bị hỏng.<br>2. Chọn "Đề xuất thanh lý".<br>3. Nhập lý do và mô tả tình trạng thiết bị.<br>4. Lưu và gửi Admin. |
| **9. Alternative Paths:** | - **Thiếu lý do:** Validation chặn không cho lưu. |
| **10. Business Rules:** | Technician chỉ đề xuất, không tự thanh lý được. Phải có lý do rõ ràng. |

### 23. UC_ViewAssignedIncidents (Xem sự cố được phân công)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | View Assigned Incidents |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Xem danh sách các sự cố đang được phân công cho mình, lọc theo trạng thái và phòng. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`TechnicianIncidentsController.Index()`) |
| **6. Pre-Condition:** | Technician đã đăng nhập. |
| **7. Post-Condition(s):** | Danh sách sự cố được hiển thị. |
| **8. Basic Path:** | 1. Vào menu "Sự cố được giao".<br>2. Hệ thống lọc IncidentReports theo `AssignedTo = currentUserId`.<br>3. Hiển thị danh sách với thông tin: Tên thiết bị, Phòng, Trạng thái, Ngày báo cáo. |
| **9. Alternative Paths:** | - **Không có sự cố:** Hiển thị "Không có sự cố nào được phân công". |
| **10. Business Rules:** | Technician chỉ xem sự cố được giao cho mình. Admin xem tất cả. |

### 24. UC_ProcessIncident (Xử lý báo cáo sự cố)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Process Incident Report |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Technician tiếp nhận và xử lý sự cố qua 2 bước: Accept (nhận việc) → Resolve (giải quyết xong). Có thể kèm lập phiếu bảo trì. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`TechnicianIncidentsController.Accept()`, `Resolve()`) |
| **6. Pre-Condition:** | Có sự cố đã được Admin phân công (`AssignedTo = currentUserId`, Status = Pending). |
| **7. Post-Condition(s):** | Báo cáo cập nhật thành Resolved. Hệ thống `<<include>>` UC_SendNotification thông báo cho Lecturer. |
| **8. Basic Path:** | 1. Xem chi tiết sự cố được phân công.<br>2. Bấm "Nhận xử lý" → Hệ thống kích hoạt `<<extend>>` UC_AcceptIncident.<br>3. Kiểm tra thiết bị thực tế.<br>4. Bấm "Đánh dấu đã giải quyết" → Kích hoạt `<<extend>>` UC_ResolveIncident.<br>5. Hệ thống gọi `<<include>>` UC_SendNotification thông báo cho Lecturer. |
| **9. Alternative Paths:** | - **Không sửa được:** Kích hoạt `<<extend>>` UC_CreateMaintenanceTicket để lập phiếu và đề xuất thanh lý. |
| **10. Business Rules:** | Trạng thái thiết bị được đồng bộ khi sự cố thay đổi. Khi Resolve: thiết bị trở về "Đang sử dụng". |

### 25. UC_AcceptIncident (Nhận xử lý sự cố)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Accept Incident |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Bước 1 trong quy trình xử lý: Technician chính thức nhận sự cố, trạng thái chuyển từ Pending → InProgress. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`TechnicianIncidentsController.Accept(id)`) |
| **6. Pre-Condition:** | Sự cố ở trạng thái Pending, đã được phân công cho Technician hiện tại. (Extend UC_ProcessIncident) |
| **7. Post-Condition(s):** | `IncidentReport.Status = "InProgress"`. Thời gian bắt đầu xử lý được ghi nhận. |
| **8. Basic Path:** | 1. Xem chi tiết sự cố.<br>2. Nhấn "Nhận xử lý".<br>3. Hệ thống cập nhật Status = InProgress trong DB. |
| **9. Alternative Paths:** | - **Sự cố không phải của mình:** Hệ thống chặn (403 Forbidden). |
| **10. Business Rules:** | Chỉ Technician được phân công mới có thể Accept. Sau khi Accept, nút "Nhận xử lý" ẩn đi, nút "Đã giải quyết" hiện ra. |

### 26. UC_ResolveIncident (Giải quyết sự cố)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Resolve Incident |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Bước cuối: Technician đánh dấu sự cố đã được giải quyết xong, kèm ghi chú kết quả. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`TechnicianIncidentsController.Resolve(id, resolutionNote)`) |
| **6. Pre-Condition:** | Sự cố ở trạng thái InProgress. (Extend UC_ProcessIncident) |
| **7. Post-Condition(s):** | `IncidentReport.Status = "Resolved"`. Hệ thống tự động gửi notification cho Lecturer báo đã xử lý xong. |
| **8. Basic Path:** | 1. Xem chi tiết sự cố đang InProgress.<br>2. Nhập ghi chú kết quả xử lý.<br>3. Nhấn "Đã giải quyết".<br>4. Hệ thống cập nhật Status = Resolved, lưu ghi chú.<br>5. Gọi `<<include>>` UC_SendNotification thông báo Lecturer. |
| **9. Alternative Paths:** | - **Không nhập ghi chú:** Validation nhắc nhập mô tả kết quả. |
| **10. Business Rules:** | Sau khi Resolved, thiết bị liên quan chuyển về trạng thái "Đang sử dụng". Notification bắt buộc phải được gửi đến Lecturer. |

### 27. UC_CreateMaintenanceTicket (Lập phiếu bảo trì)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Create Maintenance Ticket |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Lập phiếu ghi nhận việc gửi thiết bị ra đơn vị sửa chữa bên ngoài khi không thể tự sửa. |
| **4. Priority:** | Medium |
| **5. Status:** | Implemented (trong luồng xử lý sự cố) |
| **6. Pre-Condition:** | Đang trong quá trình xử lý sự cố, xác định thiết bị cần sửa ngoài. (Extend UC_ProcessIncident) |
| **7. Post-Condition(s):** | Phiếu bảo trì lưu DB, thiết bị chuyển trạng thái "Đang bảo trì". |
| **8. Basic Path:** | 1. Chọn "Lập phiếu bảo trì" từ trang xử lý sự cố.<br>2. Nhập: Đơn vị sửa chữa, phí dự kiến, ngày gửi, ngày dự kiến trả.<br>3. Lưu phiếu. |
| **9. Alternative Paths:** | - **Hủy:** Không lưu, quay lại màn hình sự cố. |
| **10. Business Rules:** | Phải ghi nhận đơn vị nhận máy và ngày dự kiến trả. |

### 28. UC_ViewEquipmentHistory (Xem lịch sử thiết bị)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | View Equipment History |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Xem toàn bộ lịch sử vòng đời thiết bị: chuyển phòng, sự cố, bảo trì, thanh lý. |
| **4. Priority:** | Low |
| **5. Status:** | Implemented (`EquipmentsController.Details(id)` — tab Lịch sử) |
| **6. Pre-Condition:** | Technician đăng nhập thành công. |
| **7. Post-Condition(s):** | Hiển thị bảng log lịch sử theo thứ tự thời gian. |
| **8. Basic Path:** | 1. Chọn thiết bị từ danh sách.<br>2. Xem trang Details → Tab "Lịch sử".<br>3. Hệ thống hiển thị: chuyển phòng (TransferHistory), sự cố (IncidentReports), bảo trì. |
| **9. Alternative Paths:** | - **Chưa có log:** Hiển thị "Thiết bị chưa có lịch sử". |
| **10. Business Rules:** | Lịch sử là Read-only, không thể sửa hay xóa. |

---

## PHẦN 4: ACTOR - LECTURER

### 29. UC_ViewRoomEquipment (Xem thiết bị phòng học)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | View Room Equipment |
| **2. Actor(s):** | Lecturer |
| **3. Summary Description:** | Giảng viên xem danh sách và tình trạng thiết bị trong các phòng học của mình. Đây cũng là **điểm khởi đầu** để Báo hỏng thiết bị. |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`LecturerRoomsController.Index()`) |
| **6. Pre-Condition:** | Lecturer đăng nhập thành công. |
| **7. Post-Condition(s):** | Danh sách thiết bị trong phòng được hiển thị. |
| **8. Basic Path:** | 1. Chọn phòng học từ dropdown.<br>2. Hệ thống lấy danh sách thiết bị có `RoomId` tương ứng.<br>3. Hiển thị dạng danh sách (Tên thiết bị, Loại, Trạng thái).<br>4. Với mỗi thiết bị đang hoạt động: hiển thị nút "Báo hỏng" → kích hoạt `<<extend>>` UC_SubmitIncident. |
| **9. Alternative Paths:** | - **Phòng trống:** Hiển thị "Hiện chưa có thiết bị nào gán vào phòng này". |
| **10. Business Rules:** | Giảng viên chỉ xem Read-only thông tin cơ bản. Không xem được chi phí hay lịch sử sửa chữa. Nút "Báo hỏng" ẩn nếu thiết bị đã có sự cố đang Pending. |

### 30. UC_SubmitIncident (Gửi báo cáo sự cố)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Submit Incident Report |
| **2. Actor(s):** | Lecturer |
| **3. Summary Description:** | Báo cáo thiết bị hỏng cho bộ phận kỹ thuật. **Chỉ được khởi tạo từ trang danh sách thiết bị phòng học** (không có entry point độc lập). |
| **4. Priority:** | High |
| **5. Status:** | Implemented (`IncidentReportsController.Create(equipmentId)`) |
| **6. Pre-Condition:** | Giảng viên đang xem danh sách thiết bị phòng. Thiết bị chưa có sự cố Pending nào. (Extend UC_ViewRoomEquipment) |
| **7. Post-Condition(s):** | `IncidentReport` được tạo trong DB với Status = Pending. Hệ thống `<<include>>` UC_SendNotification thông báo cho Technician/Admin. |
| **8. Basic Path:** | 1. Tại danh sách thiết bị phòng, nhấn "Báo hỏng" trên thiết bị cần báo.<br>2. Hệ thống điều hướng đến form báo hỏng với `equipmentId` đã chọn.<br>3. Giảng viên nhập mô tả sự cố.<br>4. Nhấn "Gửi báo cáo".<br>5. Hệ thống lưu IncidentReport (Status = Pending, ReportedBy = currentUser).<br>6. Gọi `<<include>>` UC_SendNotification cho Admin/Technician. |
| **9. Alternative Paths:** | - **Không nhập mô tả:** Validation yêu cầu nhập nội dung mô tả. |
| **10. Business Rules:** | Không được báo hỏng thiết bị đang có IncidentReport Pending. Báo cáo thuộc về người gửi — không thể gửi thay người khác. |

### 31. UC_EditIncident (Sửa báo cáo sự cố)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Edit Incident Report |
| **2. Actor(s):** | Lecturer |
| **3. Summary Description:** | Giảng viên chỉnh sửa mô tả của báo cáo sự cố đã gửi, chỉ khi báo cáo chưa được xử lý. |
| **4. Priority:** | Medium |
| **5. Status:** | Implemented (`IncidentReportsController.Edit(id)`) |
| **6. Pre-Condition:** | Báo cáo ở trạng thái Pending. Người dùng là chủ báo cáo. (Extend UC_SubmitIncident) |
| **7. Post-Condition(s):** | Mô tả sự cố được cập nhật trong DB. |
| **8. Basic Path:** | 1. Vào "Lịch sử báo cáo" → Chọn báo cáo Pending.<br>2. Nhấn "Sửa".<br>3. Chỉnh sửa nội dung mô tả.<br>4. Lưu. |
| **9. Alternative Paths:** | - **Báo cáo đã InProgress/Resolved:** Nút "Sửa" bị ẩn, không cho chỉnh sửa. |
| **10. Business Rules:** | Chỉ được sửa báo cáo ở trạng thái Pending. Chỉ chủ báo cáo mới được sửa. |

### 32. UC_CancelIncident (Hủy báo cáo sự cố)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Cancel Incident Report |
| **2. Actor(s):** | Lecturer |
| **3. Summary Description:** | Giảng viên hủy báo cáo sự cố đã gửi khi thiết bị đã tự hết lỗi hoặc báo nhầm. |
| **4. Priority:** | Medium |
| **5. Status:** | Implemented (`IncidentReportsController.Cancel(id)`) |
| **6. Pre-Condition:** | Báo cáo ở trạng thái Pending. Người dùng là chủ báo cáo. (Extend UC_SubmitIncident) |
| **7. Post-Condition(s):** | Báo cáo chuyển sang trạng thái Cancelled. Thiết bị trở về trạng thái bình thường. |
| **8. Basic Path:** | 1. Vào "Lịch sử báo cáo" → Chọn báo cáo Pending.<br>2. Nhấn "Hủy báo cáo".<br>3. Hệ thống xác nhận hành động.<br>4. Cập nhật Status = Cancelled. |
| **9. Alternative Paths:** | - **Báo cáo đã InProgress:** Không thể hủy khi Technician đang xử lý. |
| **10. Business Rules:** | Chỉ được hủy khi Status = Pending. Sau khi hủy, có thể gửi lại báo cáo mới cho cùng thiết bị đó. |

### 33. UC_TrackIncident (Theo dõi lịch sử báo cáo)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Track Incident Reports |
| **2. Actor(s):** | Lecturer |
| **3. Summary Description:** | Xem toàn bộ lịch sử các báo cáo sự cố mình đã gửi và trạng thái xử lý của từng báo cáo. |
| **4. Priority:** | Medium |
| **5. Status:** | Implemented (`IncidentReportsController.Index()`) |
| **6. Pre-Condition:** | Lecturer đăng nhập thành công. |
| **7. Post-Condition(s):** | Danh sách báo cáo được hiển thị theo thứ tự thời gian. |
| **8. Basic Path:** | 1. Vào menu "Lịch sử báo cáo".<br>2. Hệ thống lấy danh sách IncidentReports theo `ReportedBy = currentUserId`.<br>3. Hiển thị: Thiết bị, Phòng, Trạng thái, Ngày gửi, Ghi chú kết quả (nếu Resolved). |
| **9. Alternative Paths:** | - **Chưa có báo cáo nào:** Hiển thị "Bạn chưa có báo cáo sự cố nào". |
| **10. Business Rules:** | Giảng viên A không được xem báo cáo của Giảng viên B. Mỗi người chỉ thấy báo cáo của chính mình. |
