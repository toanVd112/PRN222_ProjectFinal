# Use Case Specifications
**Project:** Classroom Equipment Management System (CEMS)  
**Role:** Business Analyst / System Architect

Tài liệu này đặc tả chi tiết **toàn bộ** các Use Case của hệ thống CEMS, tất cả các Use Case đều bao gồm đầy đủ 10 thuộc tính chuẩn công nghiệp.

---

## PHẦN 1: USE CASE CHUNG (SHARED)

### 1. UC_LogIn (Đăng nhập)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Log In |
| **2. Actor(s):** | Admin, Technician, Lecturer |
| **3. Summary Description:** | Cho phép người dùng xác thực và đăng nhập vào hệ thống theo quyền hạn tương ứng. |
| **4. Priority:** | High |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Người dùng có tài khoản hợp lệ. |
| **7. Post-Condition(s):** | Phiên làm việc (Session) được khởi tạo với phân quyền đúng vai trò. |
| **8. Basic Path:** | 1. Người dùng nhập Username và Password.<br>2. Nhấn "Đăng nhập".<br>3. Hệ thống đối chiếu DB.<br>4. Hệ thống cấp Session và điều hướng đến Dashboard/Trang chủ tương ứng. |
| **9. Alternative Paths:** | - **3a. Sai thông tin:** Hệ thống hiển thị lỗi "Sai tài khoản hoặc mật khẩu".<br>- **3b. Tài khoản bị khóa:** Hệ thống hiển thị thông báo "Tài khoản của bạn đã bị khóa". |
| **10. Business Rules:** | Khóa tài khoản tạm thời 15 phút nếu nhập sai mật khẩu quá 5 lần liên tiếp. |

### 2. UC_SendNotification (Hệ thống thông báo)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Send Notification |
| **2. Actor(s):** | System |
| **3. Summary Description:** | Chức năng nội bộ tự động gửi email hoặc in-app notification cho user. |
| **4. Priority:** | High |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Nhận được lệnh kích hoạt (Trigger) từ một Use Case khác. |
| **7. Post-Condition(s):** | Thông báo được gửi đến người nhận (Technician hoặc Lecturer). |
| **8. Basic Path:** | 1. Hệ thống nhận event kích hoạt.<br>2. Tạo nội dung thông báo.<br>3. Đẩy vào hàng đợi.<br>4. Gửi tới email đích. |
| **9. Alternative Paths:** | - **4a. Gửi lỗi:** Nếu SMTP server lỗi, hệ thống lưu lại vào hàng đợi để thử lại sau. |
| **10. Business Rules:** | Nội dung thông báo phải thay đổi động dựa trên loại sự kiện (VD: Báo hỏng, Đã sửa xong). |

---

## PHẦN 2: ACTOR - ADMIN

### 3. UC_ManageUserAccount (Quản lý tài khoản)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Manage User Account |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Xem danh sách, cập nhật thông tin, phân quyền, hoặc khóa/xóa tài khoản người dùng hiện có. |
| **4. Priority:** | High |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Admin đã đăng nhập thành công. |
| **7. Post-Condition(s):** | Trạng thái hoặc thông tin tài khoản được cập nhật trong DB. |
| **8. Basic Path:** | 1. Admin truy cập "Quản lý tài khoản".<br>2. Chọn tài khoản cần thao tác.<br>3. Thay đổi thông tin (VD: Khóa tài khoản).<br>4. Nhấn "Lưu".<br>5. Hệ thống lưu lại và ghi Log. |
| **9. Alternative Paths:** | - **3a. Hủy thao tác:** Admin nhấn "Hủy", hệ thống không lưu thay đổi nào. |
| **10. Business Rules:** | Admin không thể tự khóa hoặc xóa tài khoản của chính mình. |

### 4. UC_CreateLecturerAccount (Tạo tài khoản Giảng viên)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Create Lecturer Account |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Tạo mới tài khoản cho Giảng viên. |
| **4. Priority:** | High |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Admin đã đăng nhập thành công. |
| **7. Post-Condition(s):** | Tài khoản Lecturer được tạo, email mật khẩu được gửi đi. |
| **8. Basic Path:** | 1. Chọn "Tạo tài khoản Giảng viên".<br>2. Điền thông tin (Mã GV, Tên, Email).<br>3. Nhấn "Tạo".<br>4. Hệ thống kiểm tra trùng lặp.<br>5. Sinh mật khẩu tự động.<br>6. Lưu DB.<br>7. Hệ thống tự động gửi email (Include Send Notification). |
| **9. Alternative Paths:** | - **4a. Trùng dữ liệu:** Nếu Email hoặc Mã GV đã tồn tại, hệ thống báo lỗi bôi đỏ trường tương ứng. |
| **10. Business Rules:** | Email và Mã GV phải là duy nhất (Unique). Mật khẩu phải được mã hóa (BCrypt). |

### 5. UC_CreateTechnicianAccount (Tạo tài khoản Technician)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Create Technician Account |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Tạo mới tài khoản phân quyền Technician. |
| **4. Priority:** | Medium |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Admin đã đăng nhập thành công. |
| **7. Post-Condition(s):** | Tài khoản Technician được tạo, email mật khẩu được gửi đi. |
| **8. Basic Path:** | Tương tự Basic Path của UC_CreateLecturerAccount nhưng hệ thống gán Role = Technician. |
| **9. Alternative Paths:** | Tương tự UC_CreateLecturerAccount. |
| **10. Business Rules:** | Phân quyền (Role) phải đúng là Technician. Email không được trùng lặp. |

### 6. UC_ManageRoom (Quản lý phòng học)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Manage Room |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Thêm, sửa, xóa thông tin phòng học. |
| **4. Priority:** | Medium |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Admin đã đăng nhập thành công. |
| **7. Post-Condition(s):** | Dữ liệu danh sách phòng học được cập nhật. |
| **8. Basic Path:** | 1. Admin vào Quản lý phòng.<br>2. Chọn chức năng Thêm hoặc Sửa.<br>3. Nhập dữ liệu Tên phòng, Vị trí.<br>4. Lưu và cập nhật DB. |
| **9. Alternative Paths:** | - **2a. Xóa phòng:** Nếu chọn Xóa phòng đang có chứa thiết bị, hệ thống chặn lại và báo lỗi. |
| **10. Business Rules:** | Mã phòng là duy nhất. Không được xóa phòng nếu vẫn còn thiết bị đang gán tại phòng đó. |

### 7. UC_ManageEquipmentCategory (Quản lý loại thiết bị)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Manage Equipment Category |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Quản lý danh mục loại thiết bị (Máy chiếu, Điều hòa...). |
| **4. Priority:** | Medium |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Admin đã đăng nhập thành công. |
| **7. Post-Condition(s):** | Dữ liệu Category được cập nhật. |
| **8. Basic Path:** | 1. Admin vào Quản lý danh mục.<br>2. Thêm mới danh mục.<br>3. Nhập tên danh mục.<br>4. Lưu. |
| **9. Alternative Paths:** | - **2a. Xóa danh mục:** Không cho phép xóa danh mục nếu đang có thiết bị thuộc danh mục đó. |
| **10. Business Rules:** | Tên danh mục không được để trống và không được trùng lặp. |

### 8. UC_ViewDashboard (Xem Dashboard)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | View Dashboard |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Xem biểu đồ thống kê số lượng thiết bị theo trạng thái. |
| **4. Priority:** | High |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Admin đã đăng nhập thành công. |
| **7. Post-Condition(s):** | Hiển thị dữ liệu trực quan trên giao diện. |
| **8. Basic Path:** | 1. Truy cập trang Dashboard.<br>2. Hệ thống query dữ liệu thiết bị, sự cố.<br>3. Hiển thị dưới dạng biểu đồ (Pie chart, Bar chart). |
| **9. Alternative Paths:** | - **2a. Không có dữ liệu:** Hệ thống hiển thị thông báo "Chưa có dữ liệu thống kê". |
| **10. Business Rules:** | Dữ liệu được tính toán dựa trên trạng thái thiết bị hiện tại (Real-time). |

### 9. UC_ApproveEquipmentDisposal (Phê duyệt thanh lý)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Approve Equipment Disposal |
| **2. Actor(s):** | Admin |
| **3. Summary Description:** | Phê duyệt thanh lý các thiết bị hỏng hoàn toàn. |
| **4. Priority:** | High |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Có ít nhất một thiết bị ở trạng thái "Đề xuất thanh lý". |
| **7. Post-Condition(s):** | Thiết bị chuyển sang trạng thái "Đã thanh lý". |
| **8. Basic Path:** | 1. Mở danh sách chờ phê duyệt.<br>2. Chọn thiết bị.<br>3. Bấm "Phê duyệt thanh lý".<br>4. Hệ thống cập nhật trạng thái thiết bị. |
| **9. Alternative Paths:** | - **3a. Từ chối thanh lý:** Bấm "Từ chối", hệ thống chuyển thiết bị về trạng thái "Đang chờ sửa". |
| **10. Business Rules:** | Thiết bị Đã thanh lý không còn xuất hiện trong kho sử dụng nhưng vẫn lưu log lịch sử. |

---

## PHẦN 3: ACTOR - TECHNICIAN

### 10. UC_AddUpdateEquipment (Thêm/Sửa thông tin thiết bị)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Add/Update Equipment |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Nhập kho thiết bị mới hoặc cập nhật thông tin bảo hành, serial. |
| **4. Priority:** | High |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Technician đăng nhập thành công. |
| **7. Post-Condition(s):** | Dữ liệu thiết bị lưu thành công. |
| **8. Basic Path:** | 1. Chọn Thêm thiết bị mới.<br>2. Nhập Mã TS, Tên, Ngày mua, Bảo hành.<br>3. Lưu hệ thống. |
| **9. Alternative Paths:** | - **2a. Nhập thiếu:** Bỏ trống trường bắt buộc -> Báo lỗi. |
| **10. Business Rules:** | Asset Code phải duy nhất. Technician KHÔNG có quyền bấm nút xóa để loại bỏ thiết bị khỏi DB. |

### 10b. UC_ProposeDisposal (Đề xuất thanh lý thiết bị)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Propose Equipment Disposal |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Đề xuất gửi lên Admin để bỏ thiết bị không thể sửa. |
| **4. Priority:** | Medium |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Thiết bị đang ở trạng thái lỗi hỏng. |
| **7. Post-Condition(s):** | Thiết bị đổi trạng thái "Đề xuất thanh lý". |
| **8. Basic Path:** | 1. Xem chi tiết thiết bị hỏng.<br>2. Chọn "Đề xuất thanh lý".<br>3. Nhập lý do hỏng.<br>4. Lưu và gửi Admin. |
| **9. Alternative Paths:** | - **3a. Thiếu lý do:** Không nhập lý do hỏng -> Chặn không cho lưu. |
| **10. Business Rules:** | Chỉ áp dụng cho thiết bị đã được kỹ thuật viên xác nhận là hỏng phần cứng nghiêm trọng. |

### 11. UC_ManageEquipmentLocation (Quản lý vị trí thiết bị)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Manage Equipment Location |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Điều chuyển thiết bị giữa các phòng học. |
| **4. Priority:** | High |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Thiết bị phải đang ở trạng thái sử dụng hoặc lưu kho. |
| **7. Post-Condition(s):** | Vị trí phòng mới được cập nhật, sinh log lịch sử. |
| **8. Basic Path:** | 1. Chọn thiết bị.<br>2. Nhấn "Điều chuyển".<br>3. Chọn phòng đích.<br>4. Lưu. Hệ thống tạo record trong `Transfer History`. |
| **9. Alternative Paths:** | - **3a. Trùng phòng:** Nếu phòng đích giống phòng hiện tại, báo lỗi "Thiết bị đang ở phòng này". |
| **10. Business Rules:** | Một thiết bị chỉ ở 1 phòng tại 1 thời điểm. Mọi thay đổi bắt buộc phải ghi log History. |

### 12. UC_ProcessIncident (Xử lý báo cáo sự cố)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Process Incident Report |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Tiếp nhận và cập nhật tiến độ xử lý báo cáo từ Giảng viên. |
| **4. Priority:** | High |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Có báo cáo sự cố đang ở trạng thái Pending. |
| **7. Post-Condition(s):** | Báo cáo cập nhật thành Resolved. Thông báo được gửi. |
| **8. Basic Path:** | 1. Xem danh sách sự cố.<br>2. Kiểm tra thiết bị.<br>3. Cập nhật trạng thái thành "Hoàn thành".<br>4. Hệ thống gọi `<<include>> Send Notification` cho Giảng viên. |
| **9. Alternative Paths:** | - **2a. Không sửa được:** Kích hoạt nhánh `<<extend>> Create Maintenance Ticket` để đi sửa ngoài. |
| **10. Business Rules:** | Trạng thái thiết bị phải được đồng bộ (đổi từ Chờ sửa sang Đang sử dụng) khi sự cố Hoàn thành. |

### 13. UC_CreateMaintenanceTicket (Lập phiếu bảo trì)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Create Maintenance Ticket |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Lập phiếu xuất thiết bị cho đơn vị sửa chữa bên ngoài. |
| **4. Priority:** | Medium |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Đang trong quá trình xử lý sự cố (Extend UC12). |
| **7. Post-Condition(s):** | Phiếu bảo trì tạo thành công, thiết bị chuyển trạng thái "Đang bảo trì ngoài". |
| **8. Basic Path:** | 1. Chọn Lập phiếu bảo trì.<br>2. Nhập đơn vị sửa chữa, phí dự kiến.<br>3. Lưu phiếu. |
| **9. Alternative Paths:** | - **Hủy phiếu:** Không lưu và quay lại màn hình sự cố. |
| **10. Business Rules:** | Phiếu phải ghi nhận đơn vị nhận máy và ngày dự kiến trả máy. |

### 14. UC_ViewEquipmentHistory (Xem lịch sử thiết bị)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | View Equipment History |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Xem toàn bộ nhật ký vòng đời thiết bị (chuyển phòng, sửa chữa). |
| **4. Priority:** | Low |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Technician đăng nhập thành công. |
| **7. Post-Condition(s):** | Hiển thị bảng log lịch sử. |
| **8. Basic Path:** | 1. Chọn thiết bị.<br>2. Nhấn tab Lịch sử.<br>3. Hệ thống list các log transfer, incident. |
| **9. Alternative Paths:** | - **Chưa từng có log:** Hiển thị "Thiết bị chưa có lịch sử thay đổi". |
| **10. Business Rules:** | Lịch sử thiết bị là dạng Read-only, không ai được phép chỉnh sửa hay xóa. |

### 15. UC_ExportEquipmentReport (Xuất báo cáo thiết bị)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Export Equipment Report |
| **2. Actor(s):** | Technician |
| **3. Summary Description:** | Xuất danh sách thiết bị ra file Excel/PDF để kiểm kê. |
| **4. Priority:** | Medium |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Technician đăng nhập thành công. |
| **7. Post-Condition(s):** | File Excel/PDF được tải xuống máy client. |
| **8. Basic Path:** | 1. Chọn bộ lọc (Tất cả, Theo phòng, Theo trạng thái).<br>2. Bấm Xuất Excel.<br>3. Hệ thống gen file và gửi xuống client. |
| **9. Alternative Paths:** | - **Không có dữ liệu:** File Excel trả về bảng rỗng. |
| **10. Business Rules:** | File xuất phải bao gồm cột Asset Code, Name, Room, Status. |

---

## PHẦN 4: ACTOR - LECTURER

### 16. UC_ViewRoomEquipment (Xem thiết bị phòng học)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | View Room Equipment |
| **2. Actor(s):** | Lecturer |
| **3. Summary Description:** | Giảng viên xem danh sách thiết bị đang có trong phòng dạy. |
| **4. Priority:** | High |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Lecturer đăng nhập thành công. |
| **7. Post-Condition(s):** | Hiển thị danh sách thiết bị theo phòng. |
| **8. Basic Path:** | 1. Chọn Tên phòng học.<br>2. Hệ thống query thiết bị có `RoomID` tương ứng.<br>3. Hiển thị dưới dạng List. |
| **9. Alternative Paths:** | - **Phòng trống:** Hiển thị "Hiện chưa có thiết bị nào gán vào phòng này". |
| **10. Business Rules:** | Giảng viên chỉ xem được Read-only thông tin cơ bản, không xem được chi phí hay lịch sử sửa ngoài. |

### 17. UC_SubmitIncident (Gửi báo cáo sự cố)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Submit Incident Report |
| **2. Actor(s):** | Lecturer |
| **3. Summary Description:** | Báo cáo thiết bị hỏng cho bộ phận kỹ thuật. |
| **4. Priority:** | High |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Thiết bị phải đang nằm trong phòng. |
| **7. Post-Condition(s):** | Incident Report lưu DB (Trạng thái Pending). |
| **8. Basic Path:** | 1. Tại danh sách thiết bị, bấm "Báo hỏng".<br>2. Nhập mô tả lỗi.<br>3. Gửi báo cáo.<br>4. Hệ thống kích hoạt `<<include>> Send Notification` cho Technician. |
| **9. Alternative Paths:** | - **2a. Không mô tả:** Báo lỗi yêu cầu nhập nội dung mô tả sự cố. |
| **10. Business Rules:** | Không được báo hỏng thiết bị nếu nó đã có 1 báo cáo khác đang "Pending". |

### 18. UC_TrackIncident (Theo dõi sự cố)
| Thuộc tính | Mô tả chi tiết |
| :--- | :--- |
| **1. Use Case Name:** | Track Incident Report |
| **2. Actor(s):** | Lecturer |
| **3. Summary Description:** | Xem trạng thái các báo cáo mình đã gửi. |
| **4. Priority:** | Medium |
| **5. Status:** | Detailed |
| **6. Pre-Condition:** | Lecturer đăng nhập thành công. |
| **7. Post-Condition(s):** | Danh sách báo cáo được hiển thị. |
| **8. Basic Path:** | 1. Vào Lịch sử báo cáo.<br>2. Xem danh sách, cột Trạng thái (Pending, Resolved). |
| **9. Alternative Paths:** | - **Chưa gửi:** Hiển thị "Bạn chưa có báo cáo sự cố nào". |
| **10. Business Rules:** | Giảng viên A không được phép xem nội dung báo cáo sự cố do Giảng viên B gửi. |
