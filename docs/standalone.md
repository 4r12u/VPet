# VPet Standalone

## Phạm vi

Yêu cầu: bỏ liên kết Steam khỏi source code và giữ những thành phần cần thiết để ứng dụng chạy độc lập trên Windows.

- Không khởi tạo Steam, nạp Steam SDK, dùng Steam URI hoặc yêu cầu tài khoản Steam.
- Giữ pet mặc định, animation, tương tác, ăn uống, làm việc, học, lịch hoạt động, ảnh chụp, thống kê trên máy, các profile và sao lưu local.
- Nạp mod từ thư mục `mod` cạnh ứng dụng; giữ cơ chế cho phép plugin và kiểm tra chữ ký đang có.
- Bỏ Workshop (tải/đăng/kiểm chứng), Steam Cloud, thành tích/bảng xếp hạng Steam, phòng khách qua Steam, Rich Presence và gửi chẩn đoán Steam.
- Khởi động cùng Windows và khởi động lại phải chạy trực tiếp ứng dụng, giữ profile đã chọn.
- Báo lỗi xuất thành file trên máy. Chỉ đính kèm save/settings khi người dùng chọn.
- Giữ các thành viên SteamID/IsSteamUser/GenerateAuthKey trong giao diện plugin để giảm thay đổi tương thích; giá trị trả về là 0/false. Plugin cần Steam phải được sửa riêng.
- Không thêm March 7th trong thay đổi này. Các liên kết tài liệu và tính năng/plugin cần dịch vụ riêng vẫn có thể cần Internet; standalone chỉ loại bỏ phụ thuộc Steam.

## Chạy và cài mod

Giải nén toàn bộ bản phát hành vào thư mục có quyền ghi, rồi mở `VPet-Simulator.Windows.exe`. Giữ nguyên `mod`, `GameAssets` và các DLL đi kèm. Bản phát hành self-contained mang theo .NET, không cần Visual Studio, Steam hoặc cài .NET riêng.

Đặt mod đã giải nén vào `mod/<tên mod>/`, sao cho `info.lps` nằm trực tiếp trong thư mục đó. Bật mod ở phần cài đặt và khởi động lại. Không tự động nhập mod từ thư mục Workshop cũ.

Save ở `Saves`, bản sao ở `Saves_BKP`, settings ở `Setting*.lps`. Các profile dùng hậu tố riêng. Khi nâng cấp, sao chép các thư mục save/settings và mod tự cài sang bản mới. Chưa có đồng bộ cloud tự động.

## Build không cần Visual Studio

Cần Windows và .NET SDK 10. Từ thư mục repository:

```powershell
dotnet publish VPet-Simulator.Windows/VPet-Simulator.Windows.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o ../output/releases/VPet-Standalone-win-x64
```

Luôn dùng một thư mục output mới để tránh DLL Steam còn sót từ bản build cũ. Project tự sao chép `mod` và `GameAssets`. Đóng gói toàn bộ output cùng LICENSE, các thông báo giấy phép và README của dự án gốc. Giấy phép ảnh/animation cần đọc riêng trong README gốc.

`VPet.Solution` là công cụ chỉnh cấu hình/save tùy chọn, không cần để chạy pet. Không đưa công cụ này vào bản phát hành chính khi chưa kiểm tra build và hoạt động của nó.

Với SDK 10.0.401 trên máy kiểm tra, công cụ này có lỗi Fody `Failed to resolve System.Diagnostics.DebuggerBrowsableState`. Lỗi cũng xuất hiện khi build source trước thay đổi standalone; ứng dụng chính vẫn build/publish thành công. Bản ZIP standalone không chứa công cụ đó.

## Kiểm tra

Kiểm tra bản phát hành sạch không có file/thư viện Steam, đủ tài nguyên core và .NET; chạy thử khởi động và lưu/đọc lại dữ liệu trong một thư mục riêng. Các kiểm tra tự động không thay thế việc dùng thử từng animation, mod của bên thứ ba hoặc shortcut khởi động Windows trên máy người dùng.

Chạy kiểm tra hồi quy trên một bản sao riêng bằng:

```powershell
./tests/StandaloneSmoke/Run-Smoke.ps1 -PublishDirectory '<đường dẫn tuyệt đối đến output publish>'
```

Script cần SDK 10, tạo sandbox mới và giữ lại để kiểm tra. Nó không chạy trên thư mục cài đặt đang dùng. Kiểm tra profile mặc định và profile có khoảng trắng, nạp mod local, gọi tương tác vuốt đầu/chạm thân, cho ăn, ghi save/backup, khôi phục qua process mới, quản lý save và không nạp thư viện Steam. Tên profile `March 7th` trong bài kiểm tra chỉ là tên save; không phải model March 7th.
