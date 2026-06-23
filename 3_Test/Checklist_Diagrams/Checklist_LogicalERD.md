# Checklist – Logical ERD

---

## 1. Tổng quan & Cấu trúc mô hình

| **STT** | **Hạng mục kiểm tra** | **Tiêu chí đúng / kiểm tra** | **Ghi chú / ví dụ** |
|---------|----------------------|------------------------------|---------------------|
| 1 | Mô hình là **logic**, không chứa chi tiết vật lý | ✔ Không ghi loại dữ liệu cụ thể DBMS (hoặc ghi ở mức tổng quát) | Ví dụ: "String", "Integer" thay vì "varchar(255)" |
| 2 | Mỗi thực thể (Entity) cần có tên rõ, duy nhất | ✔ Tên entity không trùng, không từ viết tắt khó hiểu | Ví dụ: Customer, Order, không dùng Cst |
| 3 | Thực thể có khóa chính (Primary Key) | ✔ Mỗi entity ít nhất 1 PK duy nhất | Nếu entity không có PK – cần xem lại mô hình |
| 4 | Không có thực thể "rỗng" không có thuộc tính | ✔ Mỗi entity ít nhất có 1 thuộc tính mô tả | Nếu entity chỉ để liên kết – cân nhắc chuyển thành relationship |
| 5 | Entity "yếu" (weak entity) nếu có được định nghĩa đúng | ✔ Có phụ thuộc vào thực thể khác, khóa phụ trợ (partial key) nếu cần | Kiểm tra ký hiệu chuẩn (double rectangle, v.v.) |
| 6 | Sự phân biệt giữa entity loại tổng quát / subtype (nếu có) | ✔ Nếu dùng kế thừa (generalization / specialization), ký hiệu đúng và ràng buộc rõ | Ví dụ: người – sinh viên / giảng viên |
| 7 | Mô hình không quá "nặng" – chia thành các phần nếu quá lớn | ✔ Có phân mảnh (sub-model) nếu mô hình lớn phức tạp | Giúp readability và maintainability |

---

## 2. Thuộc tính (Attributes) & Khóa (Keys)

| **STT** | **Hạng mục kiểm tra** | **Tiêu chí đúng / kiểm tra** | **Ghi chú / ví dụ** |
|---------|----------------------|------------------------------|---------------------|
| 1 | Tên thuộc tính rõ ràng, không trùng lắp | ✔ Không dùng tên chung mơ hồ như "Name" nếu không biết thực thể | Nếu nhiều thực thể có "Name", có thể thêm prefix: CustomerName, ProductName |
| 2 | Khóa chính được gạch chân hoặc ký hiệu rõ | ✔ Có ký hiệu chuẩn (underline) hoặc + tên | Visual Paradigm cho phép dùng ký hiệu `+id : …` |
| 3 | Khóa ngoại (Foreign Key) nếu có là thuộc tính trong entity đích quan hệ | ✔ Có FK nếu mối quan hệ 1-n hoặc n-1 | Không để FK nằm ngoài entity không rõ mối quan hệ |
| 4 | Thuộc tính đa giá trị (multi-valued) nếu có được biểu diễn đúng | ✔ Nếu cần dùng, biểu diễn dưới dạng entity phụ (associative entity) | Tránh ghi như PhoneNumbers kiểu list bên trong entity |
| 5 | Thuộc tính tổng hợp (composite attribute) nếu có biểu diễn đúng | ✔ Có cấu trúc con (ví dụ: Address → Street, City, Zip) | Nếu không cần, có thể phẳng hóa để tránh phức tạp |
| 6 | Thuộc tính bắt buộc / không bắt buộc (nullability) nếu mô hình hỗ trợ | ✔ Nếu logic yêu cầu, ghi rõ (NOT NULL) hoặc (NULL) | Ở logic ERD có thể ghi chú, nhưng không quá chi tiết vật lý |

---

## 3. Quan hệ (Relationships) & Ràng buộc (Constraints)

| **STT** | **Hạng mục kiểm tra** | **Tiêu chí đúng / kiểm tra** | **Ghi chú / ví dụ** |
|---------|----------------------|------------------------------|---------------------|
| 1 | Quan hệ giữa entity đúng & hợp lý | ✔ Mỗi quan hệ có nguồn và đích rõ ràng | Tránh quan hệ "lơ lửng" hoặc không xác định |
| 2 | Tên quan hệ rõ ràng, mang tính động từ hoặc mô tả | ✔ Dùng tên như `places`, `belongsTo`, `contains` | Tránh dùng tên trừu tượng như `rel1` |
| 3 | Bội số / số lượng (cardinality / multiplicity) đúng | ✔ Ghi rõ (1:1, 1:N, N:M) hoặc (min..max) nếu dùng notation chi tiết | Ví dụ: (0..1), (1..*), (0..*) |
| 4 | Nếu quan hệ nhiều-nhiều (N:M), sử dụng entity liên kết (associative entity) | ✔ Quan hệ N:M không trực tiếp; nếu có thuộc tính, bắt buộc dùng associative entity | Entity liên kết có PK và FK rõ ràng |
| 5 | Ràng buộc toàn phần / bán phần (total / partial participation) nếu cần | ✔ Nếu thực thể buộc phải tham gia quan hệ, dùng ký hiệu total (double line) nếu notation hỗ trợ | Nếu logic bắt buộc, nên ghi chú |
| 6 | Ràng buộc duy nhất (uniqueness), ràng buộc phụ, ràng buộc tham chiếu (referential integrity) nếu có | ✔ Nếu logic yêu cầu unique trên thuộc tính hoặc tổ hợp thuộc tính, ghi rõ | Ví dụ: mỗi khách hàng có email duy nhất |
| 7 | Quan hệ tự tham chiếu (self-relationship) nếu có đúng cách | ✔ Nếu thực thể liên hệ với chính nó, biểu diễn quan hệ đúng (ví dụ: Manager → Employee) | Ghi rõ vai trò hai đầu quan hệ (supervisor, subordinate) |

---

## 4. Ngữ nghĩa & Tính đúng đắn

| **STT** | **Hạng mục kiểm tra** | **Tiêu chí đúng / kiểm tra** | **Ghi chú / ví dụ** |
|---------|----------------------|------------------------------|---------------------|
| 1 | Không có mâu thuẫn logic giữa ràng buộc | ✔ Không có quan hệ và ràng buộc trái ngược nhau giữa cùng entity | Ví dụ: nếu ràng buộc 1:1 và 1:N cùng lúc — mâu thuẫn |
| 2 | Chi tiết logic trong quan hệ / thuộc tính phải thống nhất với yêu cầu nghiệp vụ | ✔ Thuộc tính và quan hệ phản ánh đúng yêu cầu hệ thống | Nếu yêu cầu nghiệp vụ nói một đơn có thể không có khách hàng (ví dụ guest order), thì participation phải cho phép null |
| 3 | Nếu có quan hệ kế thừa (subtype / supertype), đảm bảo các ràng buộc (disjointness, completeness) rõ ràng | ✔ Ràng buộc disjoint / overlapping, total / partial nếu dùng | Ví dụ: một người vừa có thể là sinh viên và giảng viên (overlap) hay không (disjoint) |
| 4 | Không có "chu kỳ" logic vô nghĩa trong quan hệ | ✔ Tránh mối quan hệ cách vòng lặp không hợp lý hoặc vô nghĩa | Ví dụ: entity A liên hệ với B, B với C, C lại với A không thực tế |
| 5 | Mô hình có thể chuyển sang thiết kế vật lý mà không mất tính ngữ nghĩa | ✔ Không dùng ký hiệu hoặc logic chỉ hợp lý ở ERD nhưng không thể triển khai | Ví dụ: dùng kiểu dữ liệu phức tạp (XML, JSON) trong mẫu logic mà DB quan hệ không hỗ trợ trực tiếp |
| 6 | Độ toàn vẹn tham chiếu (referential integrity) được xác định rõ | ✔ Khi xóa hoặc cập nhật, logic cascade hoặc restrict được cân nhắc | Nếu một quan hệ là bắt buộc, cần đảm bảo FK không bị null trừ khi logic cho phép |

---

## 5. Khả đọc, Dễ hiểu và Maintainability

| **STT** | **Hạng mục kiểm tra** | **Tiêu chí đúng / kiểm tra** | **Ghi chú / ví dụ** |
|---------|----------------------|------------------------------|---------------------|
| 1 | Bố cục mô hình rõ ràng, tránh đường giao nhau quá nhiều | ✔ Quan hệ được vẽ thẳng, tránh chồng lấn | Dùng layout hợp lý, tránh đường cắt ngang entity |
| 2 | Tên entity, thuộc tính, quan hệ đồng nhất theo chuẩn đặt tên trong dự án | ✔ Theo ký pháp thống nhất (PascalCase, camelCase, snake_case) | Giúp dễ chuyển sang mã nguồn hoặc DDL sau này |
| 3 | Ghi chú (note/comment) nếu logic phức tạp hoặc ngoại lệ | ✔ Nếu có ràng buộc đặc biệt (ví dụ: business rule), ghi chú giải thích | Note không thay thế ký pháp chuẩn |
| 4 | Không dùng ký hiệu "tự chế" mà gây khó hiểu nếu không có chú giải | ✔ Nếu bắt buộc dùng, kèm chú giải rõ tại legend | Ví dụ: ký hiệu đặc biệt chỉ trong dự án |
| 5 | Legend / bảng ký hiệu nếu dùng notation đặc biệt hoặc tùy chỉnh | ✔ Có phần giải thích (ví dụ cardinality notation, participation notation) | Đặc biệt cần nếu team khác đọc sơ đồ |
| 6 | Mô hình sạch – không có đối tượng "mồ côi" (entity hoặc quan hệ không dùng) | ✔ Entity không có quan hệ nào hoặc quan hệ không kết nối – xem lại | Nếu có, loại bỏ hoặc giải thích lý do tồn tại |

---

## 6. Liên kết với các Mô hình / Tài liệu khác

| **STT** | **Hạng mục kiểm tra** | **Tiêu chí đúng / kiểm tra** | **Ghi chú / ví dụ** |
|---------|----------------------|------------------------------|---------------------|
| 1 | Consistency với **mô hình nghiệp vụ / domain model** | ✔ Các thực thể và quan hệ phải trùng khớp với domain concept | Nếu domain có "Customer", ERD cũng dùng tên đó, không dùng "ClientX" khác |
| 2 | Kiểm tra tương hợp với **sơ đồ Class / UML** nếu có | ✔ Các class dữ liệu và entity tương ứng logic | Nếu class có attribute "birthDate", entity phải có thuộc tính tương ứng |
| 3 | Mapping sang thiết kế vật lý (DDL) phải dễ thực hiện | ✔ FK, PK, constraints đủ thông tin để chuyển sang DDL | Nếu logic ERD thiếu thông tin quan trọng, việc sinh ra schema sẽ khó |
| 4 | Tài liệu bổ trợ (data dictionary, constraints, business rules) kèm theo | ✔ Có bảng mô tả từng entity, attribute, quan hệ và ràng buộc | Giúp người khác hiểu lý do thiết kế |
| 5 | Kiểm tra ảnh hưởng lên performance / scaling nếu cần (ở mức logic) | ✔ Nếu có nhiều quan hệ N:M, xem cách tổ chức để tránh đa truy vấn phức tạp | Mặc dù logic ERD chưa phải physical, nhưng nên lưu ý trước |
