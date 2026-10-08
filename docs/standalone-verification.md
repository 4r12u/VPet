# Kiểm tra VPet Standalone — 2026-10-08

Baseline source: `c3420bb77d6d00ed5fabf975a0945c4f6179bba3`.

- Publish ứng dụng chính Release x64, self-contained: thành công, .NET SDK 10.0.401. Các cảnh báo Microsoft.CSharp/X509Certificate2 đã có trong source, không có lỗi build.
- Kiểm tra hồi quy trong sandbox: 4 process đều qua (`init` và `restore` cho profile mặc định và profile `March 7th`). Đây là tên profile thử nghiệm, pet vẫn là pet mặc định.
- Đã kiểm tra nạp pet/animation/food core, nạp mod local được bật, tương tác đầu/thân cập nhật thống kê, cho ăn cập nhật trạng thái, lưu local và tạo backup, đọc lại tên/tiền/thống kê từ process mới, khôi phục backup, lọc save theo profile, báo cáo không tự đính kèm dữ liệu, không nạp DLL Steam.
- File EXE publish được chạy trực tiếp trong bản sao riêng trong 20 giây: process phản hồi, nạp `coreclr.dll` ngay trong thư mục ứng dụng, WPF và Skia; không nạp Steam/Facepunch. Process được khởi động ẩn và đóng sau phép thử.
- Thư viện Facepunch và DLL native Steam đã được loại khỏi project/bản publish. Danh sách file và kích thước tài nguyên `mod`/`GameAssets` khớp source.
- `git diff --check` và kiểm tra cú pháp script PowerShell: qua.
- `VPet.Solution` chưa build được vì lỗi Fody `Failed to resolve System.Diagnostics.DebuggerBrowsableState`. Đã build một bản source baseline riêng và tái hiện cùng lỗi, xác nhận đây là lỗi có trước. Công cụ tùy chọn này không nằm trong ZIP ứng dụng chính.

Chưa kiểm tra thủ công từng animation, tất cả thao tác trên giao diện, mod có code của bên thứ ba hay thực hiện đăng nhập Windows để thử shortcut. Không thay đổi save/thiết lập hoặc shortcut Startup hiện có của người dùng trong các phép thử.

## Standards

Hard standards violations: none found beyond formatter-enforced style.

The startup shortcut argument is now quoted, and the empty `VUP_Click` / `cb_NoCheat_Unchecked` handlers plus their bindings are removed. Restart also preserves the explicit default profile through the shared argument property. These findings are resolved.

The added smoke checks meaningfully exercise local mod loading, interaction statistics, feeding, save persistence across processes, backup restoration, profile isolation, and absence of loaded Steam libraries. The suggested sandbox marker check was moved before writing the report file.

## Spec

The profile-preservation finding is resolved: restart and startup shortcuts always pass an explicit profile argument, including the default profile, and the constructor normalizes the empty argument correctly. Quoting preserves profile names containing spaces.

The reviewed changes retain local mod enumeration and plugin consent/signature checks, local save/backup behavior, and local report export with save inclusion unchecked by default. Active Steam initialization, Workshop calls, Cloud, leaderboard, lobby, telemetry and Rich Presence paths are removed.

Review summary: Standards — no unresolved findings; Spec — no unresolved findings. Runtime checks above cover the application; the optional helper's baseline build limitation remains documented separately.
