# Software Development Project Specification

## 1. Môn học

**Mã môn học:** PRU222

## 2. Số lượng thành viên

**Số lượng thành viên nhóm:** 3

## 3. Tên dự án

**Classroom Equipment Management System (CEMS)**
*(Hệ thống quản lý thiết bị trong phòng học)*

**Công nghệ áp dụng:** ASP.NET MVC, C#, SQL Server
**Hướng kiến trúc:** Model - View - Controller (MVC)

---

## 4. Tổng quan dự án

### 4.1. Giới thiệu / Đặt vấn đề

Hiện nay, việc quản lý thiết bị trong các phòng học (máy chiếu, máy tính, điều hòa, bàn ghế, loa, micro...) tại nhiều trường học/trung tâm đào tạo vẫn được thực hiện thủ công: ghi sổ, theo dõi qua Excel, báo hỏng bằng miệng hoặc giấy. Điều này dẫn đến nhiều vấn đề: không biết chính xác thiết bị nào đang ở phòng nào, thiết bị hỏng không được báo cáo và sửa chữa kịp thời, không có lịch sử bảo trì để đánh giá tuổi thọ thiết bị, kiểm kê cuối kỳ tốn nhiều thời gian và dễ sai sót.

CEMS ra đời nhằm số hóa toàn bộ quy trình này: từ việc nhập danh sách thiết bị, gán thiết bị vào phòng, theo dõi tình trạng, tiếp nhận báo hỏng, xử lý sửa chữa, đến thống kê báo cáo cho ban quản lý.

### 4.2. Tóm tắt dự án

CEMS là một ứng dụng web được xây dựng trên nền ASP.NET MVC, ngôn ngữ C#, cơ sở dữ liệu SQL Server. Hệ thống đóng vai trò là công cụ trung tâm để quản lý toàn bộ thiết bị vật chất trong các phòng học của một đơn vị (trường học, trung tâm, khoa...).

Ba nhóm người dùng chính tương tác với hệ thống:

- **Quản trị viên (Admin):** quản lý toàn bộ dữ liệu nền (phòng học, loại thiết bị, tài khoản người dùng) và phê duyệt các yêu cầu quan trọng.
- **Nhân viên quản lý thiết bị (Technician):** thực hiện các nghiệp vụ hằng ngày như cấp phát thiết bị vào phòng, xử lý báo hỏng, lập phiếu sửa chữa, kiểm kê định kỳ.
- **Giảng viên (Lecturer):** xem tình trạng thiết bị trong phòng mình dạy, gửi báo cáo khi phát hiện thiết bị hỏng hoặc thiếu.

### 4.3. Các chức năng của dự án

**Nhóm chức năng cho Admin:**
- Quản lý tài khoản người dùng và phân quyền (Admin, Nhân viên, Giảng viên)
- Quản lý danh mục phòng học (thêm/sửa/xóa phòng, sức chứa, vị trí, loại phòng)
- Quản lý danh mục loại thiết bị (máy chiếu, máy tính, điều hòa...)
- Xem báo cáo thống kê tổng thể, dashboard tình trạng thiết bị toàn trường
- Phê duyệt thanh lý thiết bị hỏng không thể sửa

**Nhóm chức năng cho Nhân viên quản lý thiết bị:**
- Quản lý thông tin thiết bị (mã thiết bị, tên, ngày nhập, nhà cung cấp, thời hạn bảo hành, tình trạng)
- Gán/điều chuyển thiết bị vào/giữa các phòng học
- Tiếp nhận và xử lý báo cáo hỏng từ giảng viên (chuyển trạng thái: chờ xử lý → đang sửa → đã sửa/không sửa được)
- Lập phiếu bảo trì - sửa chữa, ghi nhận chi phí và đơn vị sửa chữa
- Xem lịch sử thay đổi trạng thái/sửa chữa của từng thiết bị

**Nhóm chức năng cho Giảng viên:**
- Xem danh sách và tình trạng thiết bị trong phòng học được phân công
- Gửi báo cáo sự cố/hỏng thiết bị (kèm mô tả, có thể đính kèm ảnh)
- Theo dõi trạng thái xử lý báo cáo mình đã gửi

**Nhóm chức năng chung của hệ thống:**
- Đăng nhập, quản lý phiên làm việc theo vai trò (Authentication/Authorization)
- Quản lý hồ sơ cá nhân (Manage Personal Profile) - xem và cập nhật thông tin cá nhân cơ bản
- Đổi mật khẩu (Change Password) và Quên mật khẩu (Forgot Password)
- Thông báo (email hoặc thông báo trong hệ thống) khi báo cáo được xử lý, khi thiết bị gần hết bảo hành
- Tìm kiếm, lọc thiết bị theo nhiều tiêu chí (phòng, loại, tình trạng, ngày nhập)
- Xuất báo cáo (Excel/PDF) cho kiểm kê hoặc báo cáo định kỳ

### 4.4. Vision and Scope

**Vision Statement:**
Xây dựng một hệ thống quản lý thiết bị phòng học tập trung, giúp đơn vị quản lý nắm bắt chính xác, theo thời gian thực tình trạng và vị trí của toàn bộ thiết bị, rút ngắn thời gian xử lý sự cố, giảm thất thoát và nâng cao hiệu quả sử dụng tài sản.

**Mục tiêu kinh doanh (Business Objectives):**
- Số hóa 100% quy trình theo dõi thiết bị, loại bỏ sổ sách/Excel thủ công
- Giảm thời gian từ lúc báo hỏng đến lúc xử lý xong
- Cung cấp dữ liệu chính xác để hỗ trợ ra quyết định mua sắm/thanh lý thiết bị
- Tăng tính minh bạch, có thể truy vết lịch sử của từng thiết bị

**Phạm vi trong dự án (In-scope):**
- Quản lý phòng học, thiết bị, tài khoản người dùng theo 3 vai trò: Admin, Technician, Lecturer
- Quy trình báo hỏng → xử lý → đóng yêu cầu
- Báo cáo thống kê
- Lịch sử thay đổi trạng thái thiết bị (audit trail)

**Phạm vi ngoài dự án (Out-of-scope):**
- Tích hợp cảm biến IoT để tự động phát hiện thiết bị hỏng
- Ứng dụng di động riêng (chỉ làm web, có thể responsive)
- Tích hợp thanh toán/mua sắm thiết bị qua hệ thống
- Quản lý đặt phòng/lịch học

**Các bên liên quan (Stakeholders):**
- Ban quản lý cơ sở vật chất/phòng thiết bị của trường (chủ đầu tư giả định)
- Nhân viên kỹ thuật/quản lý thiết bị (người dùng chính)
- Giảng viên (người dùng gián tiếp, báo cáo sự cố)
- Giảng viên hướng dẫn đồ án (người đánh giá)

---

## 5. Các Actors của dự án

- **Admin (Quản trị viên):** có quyền cao nhất, quản lý dữ liệu nền và phê duyệt.
- **Technician (Nhân viên quản lý thiết bị):** thực hiện nghiệp vụ vận hành hằng ngày.
- **Lecturer (Giảng viên):** người dùng cuối, chủ yếu xem và báo cáo sự cố.
- **System (Hệ thống — tùy chọn):** đại diện cho các use case tự động kích hoạt theo thời gian/điều kiện (nhắc hết bảo hành, đánh dấu báo cáo quá hạn).

---

## 6. Use Case của dự án

### 6.1. Use Case theo Actor

**Admin:**
- Đăng nhập hệ thống, Quản lý hồ sơ cá nhân, Đổi/Quên mật khẩu
- Quản lý tài khoản người dùng (bao gồm xem danh sách, thêm mới, chỉnh sửa, khóa/xóa tài khoản và phân quyền)
- Quản lý danh mục phòng học
- Quản lý danh mục loại thiết bị
- Xem dashboard/báo cáo thống kê tổng thể
- Phê duyệt thanh lý thiết bị

**Technician (Nhân viên quản lý thiết bị):**
- Đăng nhập hệ thống, Quản lý hồ sơ cá nhân, Đổi/Quên mật khẩu
- Quản lý thông tin thiết bị (thêm, sửa, đề xuất thanh lý)
- Gán thiết bị vào phòng học lần đầu
- Điều chuyển thiết bị giữa các phòng
- Tiếp nhận báo cáo sự cố từ giảng viên
- Xử lý báo cáo sự cố (cập nhật trạng thái xử lý)
- Lập phiếu bảo trì/sửa chữa
- Xem lịch sử thay đổi của thiết bị
- Tìm kiếm, lọc, xuất báo cáo thiết bị

**Lecturer (Giảng viên):**
- Đăng nhập hệ thống, Quản lý hồ sơ cá nhân, Đổi/Quên mật khẩu
- Xem danh sách và tình trạng thiết bị trong phòng học
- Gửi báo cáo sự cố/thiết bị hỏng
- Theo dõi trạng thái xử lý báo cáo đã gửi

### 6.2. Quan hệ Include/Extend giữa các Use Case

- "Quản lý hồ sơ cá nhân" **extend** "Đổi mật khẩu" (người dùng truy cập trang đổi mật khẩu từ màn hình thông tin cá nhân)
- "Quản lý tài khoản người dùng" **extend** "Tạo tài khoản Giảng viên", "Tạo tài khoản Technician", "Xem chi tiết tài khoản", "Cập nhật tài khoản", "Khóa/Xóa tài khoản"
- "Quên mật khẩu" **include** "Gửi thông báo" (gửi email liên kết đặt lại mật khẩu)
- "Gửi báo cáo sự cố" **include** "Gửi thông báo" (tự động thông báo cho nhân viên quản lý thiết bị khi có báo cáo mới)
- "Xử lý báo cáo sự cố" **include** "Gửi thông báo" (thông báo lại cho giảng viên khi xử lý xong)
- "Xử lý báo cáo sự cố" **extend** "Lập phiếu bảo trì/sửa chữa" (chỉ xảy ra trong nhánh điều kiện cần sửa chữa, không phải mọi báo cáo đều cần lập phiếu)
- "Gán thiết bị vào phòng lần đầu" và "Điều chuyển thiết bị giữa các phòng" có thể gộp thành use case tổng **"Quản lý vị trí thiết bị"** (cùng logic cập nhật vị trí hiện tại + ghi lịch sử, chỉ khác điều kiện đầu vào)

---

## 7. Business Rules

1. Mỗi thiết bị phải có một mã định danh duy nhất (asset code) không trùng lặp trong toàn hệ thống.
2. Tại một thời điểm, mỗi thiết bị (không di động) chỉ được gán cho duy nhất một phòng học. Quan hệ giữa Equipment và Room là một-nhiều (one-to-many); mọi thay đổi vị trí phải được ghi nhận qua bảng lịch sử chuyển phòng (Transfer History), không cập nhật trực tiếp và tùy ý lên trường vị trí hiện tại.
3. Thiết bị chỉ tồn tại ở một trong các trạng thái xác định: Đang sử dụng, Đang chờ sửa, Đang bảo trì, Đã thanh lý/Ngừng sử dụng. Việc chuyển trạng thái phải tuân theo luồng hợp lệ (không thể chuyển trực tiếp từ "Đang sử dụng" sang "Đã thanh lý" mà không qua "Đang chờ sửa" nếu nguyên nhân là hỏng hóc).
4. Chỉ Admin có quyền xóa (phê duyệt thanh lý) thiết bị. Nhân viên quản lý thiết bị có quyền thêm/sửa thông tin và đề xuất thanh lý thiết bị; Giảng viên chỉ có quyền xem và gửi báo cáo sự cố.
5. Mọi thay đổi trạng thái thiết bị (gán phòng, sửa chữa, thanh lý) phải được ghi log đầy đủ (ai thực hiện, thời gian) và không được xóa, phục vụ việc truy vết sau này.
6. Một báo cáo sự cố sau khi được nhân viên tiếp nhận phải được xử lý và đóng lại trong một khoảng thời gian quy định (ví dụ 3-5 ngày làm việc); nếu quá hạn hệ thống cần đánh dấu cảnh báo.
7. Thiết bị sắp hết thời hạn bảo hành (ví dụ còn 30 ngày) cần được hệ thống tự động đánh dấu/nhắc nhở cho nhân viên quản lý.
8. Tài khoản người dùng phải được xác thực (đăng nhập) trước khi truy cập bất kỳ chức năng nào; mỗi vai trò chỉ thấy và thao tác được trên các chức năng được phân quyền tương ứng.
