# Zombie Slayer (Zombie War) - Unity 3D Survival Shooter

## Giới thiệu (Introduction)
**Zombie Slayer** là một tựa game Top-Down Survival Shooter được phát triển trên nền tảng Unity. Dự án này được thiết kế dựa trên yêu cầu đánh giá năng lực (Assessment Test) với mục tiêu phô diễn các kỹ năng lập trình Gameplay, AI, Shader, UI/UX và tối ưu hóa hiệu suất trên thiết bị di động (Mobile).

Dự án không chỉ hoàn thành **100% các yêu cầu cơ bản** của đề bài mà còn được mở rộng với rất nhiều **tính năng nâng cao (Bonus Features)** như hệ thống kỹ năng Roguelite, tích hợp Firebase, hệ thống Boss có bộ kỹ năng phức tạp, và tối ưu hóa chuyên sâu bằng Object Pooling nhằm mang lại trải nghiệm của một sản phẩm thương mại thực tế.

---

## 🎮 Bàn giao (Deliverables)
- **Mã nguồn (Source Code):** [Github Repository](#) *(Bạn đang ở đây)*
- **File Cài đặt (APK):** [Tải File APK Tại Đây](#) *(https://drive.google.com/drive/folders/106yxHNk9OfcpE8oanZVlSMt074Sp7ri3?usp=drive_link)*
- **Video Gameplay:** [Xem trên YouTube](#) *(https://youtu.be/UM0Md3VSabA)*

---

## Các Yêu Cầu Đề Bài Đã Hoàn Thành (Core Requirements)

### 1. Camera & Góc nhìn
- Cài đặt **Cinemachine** để Camera theo dõi (Follow) nhân vật mượt mà.
- Thiết lập góc nhìn Top-down (từ trên xuống) chuẩn xác để quan sát chiến trường tứ phía.

### 2. Điều khiển (Controls)
- Tích hợp **Virtual Joystick** trên màn hình UI để điều khiển nhân vật, tối ưu cho thao tác chạm của Mobile.

### 3. Hệ thống Nhân vật (Soldier)
- Xử lý Animation chuyên sâu bằng **Animation Layers** và **Avatar Mask**, tách biệt chuyển động thân trên (cầm súng/bắn) và thân dưới (di chuyển), giúp nhân vật vừa chạy vừa bắn tự nhiên.
- Dùng **Blend Trees** để mượt mà hóa chuyển động giữa các hướng và dùng để phân luồng quyết định sử dụng animation nào dựa vào vũ khí.
- Có hiệu ứng hạt (Particle) báo mất máu khi bị Zombie tấn công.

### 4. Hệ thống Kẻ địch (Zombies)
- **AI Tự động:** Sử dụng Raycast giúp quái dễ dàng né các vật thể cản đường, để quái liên tục dò đường tìm đến vị trí người chơi.
- **VFX & Shader:** Tạo Custom Shader để làm hiệu ứng Zombie chớp trắng (Hit Flash) khi trúng đạn và hiệu ứng tan biến dần (Dissolve) khi chết.

### 5. Hệ thống Vũ khí (Weapons)
- Cài đặt hệ thống 3 loại súng với các thông số khác nhau (Sát thương, tốc độ bắn).
- Nút chuyển súng (Switch Button) linh hoạt.
- Tích hợp đầy đủ **Particle System** tia lửa súng (Muzzle flash), và hiệu ứng nổ khi đạn chạm mục tiêu.
- Thêm hiệu ứng **Camera Shake** và súng giật (Recoil) giúp tăng cảm giác bắn (Game Feel).

### 6. Lựu đạn (Bombs)
- Người chơi có thể ném lựu đạn. Khi nổ tạo ra lực vật lý hất văng Zombie (Explosion Force) và gây sát thương diện rộng.

### 7. Thiết kế Màn chơi (Level Design & Pacing)
- **Thời lượng:** Mỗi level được cân bằng để diễn ra trong tầm 3 phút.
- **Level 1:** Mặt đất bằng phẳng, hệ thống sinh chướng ngại vật ngẫu nhiên.
- **Level 2:** Nhịp điệu dồn dập và xuất hiện **ZOMBIE KHỔNG LỒ (Boss)**.
- **Nhịp độ (Pacing):** Game tự động đẩy nhanh tốc độ sinh quái (Spawn Rate) theo thời gian.

### 8. Âm thanh & Hạt hình ảnh (Audio & Particles)
- Tích hợp đầy đủ hệ thống Audio (BGM, SFX bắn súng, nổ, tiếng quái vật, tiếng nâng cấp UI).
- Đẩy mạnh yếu tố Visual bằng hạt (Particles) cho từng hành động.

---

## Các Tính Năng Nâng Cao Đã Làm Thêm (Bonus & Advanced Features)
*Để chứng minh năng lực và niềm đam mê với dự án, tôi đã chủ động thiết kế và lập trình thêm các tính năng quan trọng sau đây nhằm đưa dự án vươn xa hơn mức yêu cầu cơ bản.*

### 1. Hệ thống Tiến trình kiểu Roguelite (Progression System)
- Khi chết, Zombie rơi ra các viên **Gems Kinh Nghiệm (EXP)** với nhiều mức giá trị được quản lý bằng ScriptableObject.
- **Thẻ Nâng Cấp (Card System):** Khi lên cấp, màn hình hiện ra 3 thẻ bài ngẫu nhiên cho phép người chơi chọn nâng cấp chỉ số vĩnh viễn (Attack, HP, Defense) cho màn chơi đó.

### 2. Hệ Thống Kỹ Năng Sinh Tồn Đa Dạng (Survivor Skills)
- Xây dựng một hệ thống đa kỹ năng (hoạt động độc lập cùng lúc), có thể nâng cấp sức mạnh, số lượng và phạm vi theo cấp độ (Level):
  - **Air Support (Gọi Viện Trợ):** Máy bay bay từ rìa màn hình, quét qua bản đồ và xả mưa đạn diện rộng.
  - **Spinner / UAV (Vệ Tinh Bảo Vệ):** Các quả cầu/Drone xoay liên tục quanh người chơi, càn quét bất kỳ Zombie nào tới gần.
  - **Aura (Vòng Sát Thương):** Tỏa ra vòng hào quang đốt máu kẻ địch xung quanh.
  - **Landmine (Trực Thăng rải mìn):** Tự động đặt mìn bẫy trên bản đồ, tạo sát thương nổ vật lý khi quái vật đạp trúng.

### 3. Tích hợp Backend (Firebase & Google Sign-in)
- Đã cài đặt **Firebase SDK** và **Google Sign-In**.
- Game tự động lưu trữ lượng Vàng (Gold), chỉ số của người chơi (tấn công, phòng thủ, máu,...), màn chơi hiện tại,... lên cơ sở dữ liệu đám mây **Firestore**, đảm bảo dữ liệu không bị mất và có thể đồng bộ trên nhiều thiết bị.

### 4. Boss Khổng Lồ Với AI Phức Tạp (Advanced Boss AI)
Boss ở Level 2 không chỉ to hơn mà được lập trình State Machine riêng biệt với bộ 3 kỹ năng gồm các SFX cho từng kỹ năng:
- **Jump Slam:** Nhảy lên không trung và đập mạnh xuống đất gây sát thương nổ.
- **Charge Dash:** Lao tới (Dash) cực nhanh về phía người chơi, để lại vệt sáng (Trail).
- **Summon Minions:** Khóa hoạt ảnh (Animation Lock) và triệu hồi thêm các Zombie nhỏ.

### 5. Tối Ưu Hóa Hiệu Suất Cao (Performance Optimization)
- **Object Pooling Toàn Diện:** Mọi thứ từ Zombie, Đạn, Lựu Đạn, EXP Gems đến Hit Particles đều được dùng chung Object Pooling để loại bỏ hoàn toàn hiện tượng giật lag (GC Spikes) do Instantiate/Destroy liên tục, đảm bảo **60 FPS** cho Mobile.
- **MaterialPropertyBlock:** Các hiệu ứng Shader (chớp trắng, dissolve) của hàng chục quái vật trên màn hình được xử lý bằng `MaterialPropertyBlock`, giúp tránh việc sinh ra hàng loạt các biến thể Material làm nghẽn RAM.

### 6. Giao Diện Người Dùng & Tương Thích (UI/UX & Multi-resolution)
- **World-Space UI:** Tích hợp thanh máu (Health Bar) hiển thị nổi trên đầu nhân vật và quái, giúp người chơi dễ dàng theo dõi giao tranh.
- Canvas được setup chuẩn xác để UI Scale động hỗ trợ mọi kích thước và tỉ lệ màn hình Android (Notch, Tablet, Phone).

---

## Nền tảng Kỹ thuật & Kiến trúc
- **Game Engine:** Unity 3D
- **Ngôn ngữ:** C#
- **Nền tảng đích:** Android
- **Mẫu thiết kế (Design Patterns):** 
  - *Singleton* (Manager classes)
  - *State Machine* (AI Behavior)
  - *Object Pooling* (Tối ưu bộ nhớ)
- **Công cụ bên thứ 3:** Firebase, Google Play Games Services, Cinemachine.

---
*Cảm ơn anh/chị đã dành thời gian trải nghiệm và đánh giá dự án này. Em đã dồn rất nhiều tâm huyết để chăm chút từ Gameplay, Feel, đến tối ưu hóa Code. Rất hy vọng sẽ có cơ hội được tham gia phỏng vấn để trình bày chi tiết hơn!*
