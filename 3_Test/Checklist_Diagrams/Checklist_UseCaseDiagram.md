# CHECKLIST REVIEW USE CASE DIAGRAM

| STT | Nhóm kiểm tra | Tiêu chí / Hạng mục review | Mục đích kiểm tra | Câu hỏi / Hướng dẫn đánh giá | Ghi chú / Phát hiện |
|---|---|---|---|---|---|
| **I. KÝ PHÁP (Notation Check)** | | | | | |
| 1 | Ký pháp | Actor được vẽ đúng ký pháp UML | Đảm bảo đúng chuẩn UML | Có sử dụng đúng biểu tượng cho Actor, Use Case, và quan hệ không? | |
| 2 | Ký pháp | Tên actor và use case rõ ràng, ngắn gọn | Giúp người đọc hiểu nhanh ý nghĩa | Tên có mô tả hành động hoặc vai trò cụ thể, tránh từ mơ hồ? | |
| 3 | Ký pháp | Ký hiệu «include», «extend», «generalization» đúng vị trí và định dạng | Tránh sai biểu tượng / ký hiệu | Các mối quan hệ này có được ghi đúng cú pháp UML và đặt gần đường nối tương ứng không? | |
| 4 | Ký pháp | Hướng mũi tên đúng quy ước | Xác định chiều quan hệ rõ ràng | Mũi tên trong «include» và «extend» có hướng đúng (từ use case gốc tới use case được bao gồm / mở rộng)? | |
| 5 | Ký pháp | Các đường liên kết không chồng chéo, sơ đồ bố trí rõ ràng | Đảm bảo sơ đồ dễ đọc | Có đường chéo phức tạp hoặc chồng chéo gây nhầm lẫn không? | |
| **II. CÚ PHÁP (Syntactic Check)** | | | | | |
| 6 | Cú pháp | Mỗi use case phải có ít nhất một actor liên quan | Đảm bảo tính tương tác | Có use case nào không actor nào tham gia không? | |
| 7 | Cú pháp | Mỗi actor phải có ít nhất một use case liên kết | Tránh actor dư thừa | Có actor nào không thực hiện use case nào không? | |
| 8 | Cú pháp | Quan hệ include / extend hợp lý và đúng logic | Đảm bảo cấu trúc hợp lệ | «include» khi logic luôn xảy ra; «extend» khi chỉ thỉnh thoảng — có đúng không? | |
| 9 | Cú pháp | Mối quan hệ generalization giữa actors hoặc use cases đúng logic | Kiểm tra tính kế thừa hợp lý | Actor con có thực sự là một loại cụ thể của actor cha? | |
| 10 | Cú pháp | Mỗi use case nên thể hiện một mục tiêu hoàn chỉnh cho actor | Giữ đúng bản chất của use case | Use case có kết thúc với giá trị hữu ích cho actor không? | |
| **III. NGỮ NGHĨA (Semantic Check)** | | | | | |
| 14 | Ngữ nghĩa | Actor thể hiện đúng vai trò người / hệ thống bên ngoài | Tránh nhầm lẫn thực thể nội bộ | Có actor nào thực ra là thành phần bên trong hệ thống không? | |
| 15 | Ngữ nghĩa | Use case phản ánh đúng mục tiêu nghiệp vụ | Kiểm tra tính đúng đắn nội dung | Tên và ý nghĩa của use case có khớp với yêu cầu nghiệp vụ không? | |
| 16 | Ngữ nghĩa | Mối quan hệ giữa các use case thể hiện đúng ý nghĩa nghiệp vụ | Logic tương tác hợp lý | «include» dùng cho hành vi bắt buộc, «extend» cho hành vi tùy chọn – có đúng không? | |
| 17 | Ngữ nghĩa | Không thiếu use case quan trọng từ yêu cầu | Tránh thiếu nghiệp vụ | Có nghiệp vụ nào chưa được thể hiện trong sơ đồ không? | |
| **IV. GHI CHÚ & QUẢN LÝ** | | | | | |
| 22 | Ghi chú | Có ghi chú (note) cho các mối quan hệ đặc biệt | Giúp giải thích chi tiết | Có dùng ghi chú UML (<<note>>) cho phần cần làm rõ? | |
| 23 | Ghi chú | Có tiêu đề sơ đồ, mô tả phạm vi và phiên bản | Quản lý mô hình | Có thông tin version, ngày cập nhật, người tạo? | |
| 24 | Ghi chú | Sơ đồ thể hiện ranh giới hệ thống rõ ràng (system boundary box) | Xác định phạm vi mô hình | Có hộp bao quanh thể hiện phạm vi hệ thống không? | |
| 25 | Ghi chú | Có liên kết tài liệu chi tiết (use case description, specification) | Liên kết tài liệu yêu cầu | Mỗi use case trong sơ đồ có tài liệu chi tiết đi kèm? | |
