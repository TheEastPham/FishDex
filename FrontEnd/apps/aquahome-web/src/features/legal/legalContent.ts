/**
 * Nội dung Chính sách quyền riêng tư và Điều khoản sử dụng.
 *
 * Để dạng `{vi, en}` trong file thay vì nhét vào `vi.ts`/`en.ts` — theo đúng cách
 * `ReleasePage` đang làm với nội dung dài. Locale file dành cho nhãn UI, không dành cho
 * văn bản nhiều đoạn.
 *
 * ⚠️ BẢN NHÁP, CHƯA QUA RÀ SOÁT PHÁP LÝ. Nội dung được viết theo đúng những gì hệ thống
 * thực sự thu thập và gửi đi (đã đối chiếu entity, service và biến môi trường), nhưng
 * người viết không phải luật sư. Đọc lại trước khi công bố, nhất là nếu sau này thu phí.
 *
 * Google yêu cầu hai URL này để chuyển OAuth consent screen từ `Testing` sang
 * `In production` — thiếu chúng thì refresh token YouTube hết hạn mỗi 7 ngày.
 */

export type Lang = { vi: string; en: string };

export interface LegalSection {
  heading: Lang;
  paragraphs?: Lang[];
  bullets?: Lang[];
}

export interface LegalDoc {
  title: Lang;
  updated: string;
  intro: Lang;
  sections: LegalSection[];
}

const LIEN_HE = 'admin@fishlover.org';

export const PRIVACY: LegalDoc = {
  title: { vi: 'Chính sách quyền riêng tư', en: 'Privacy Policy' },
  updated: '2026-08-23',
  intro: {
    vi: `FishLover là một dự án cá nhân dành cho người chơi cá cảnh, không phải sản phẩm của một công ty. Trang này nói rõ chúng tôi lưu gì, gửi đi đâu, và bạn kiểm soát được những gì. Viết thẳng, không vòng vo.`,
    en: `FishLover is a personal project for aquarium hobbyists, not a company product. This page states plainly what we store, where it goes, and what you control. No boilerplate.`,
  },
  sections: [
    {
      heading: { vi: '1. Dữ liệu tài khoản', en: '1. Account data' },
      paragraphs: [
        {
          vi: 'Đăng ký hiện theo hình thức mời, cần mã lời mời. Khi tạo tài khoản chúng tôi lưu:',
          en: 'Registration is invitation-only and requires an invitation code. When you create an account we store:',
        },
      ],
      bullets: [
        { vi: 'Địa chỉ Gmail (hiện chỉ nhận @gmail.com) — dùng để đăng nhập và gửi email hệ thống', en: 'Gmail address (only @gmail.com is accepted) — used to sign in and send system email' },
        { vi: 'Họ và tên', en: 'First and last name' },
        { vi: 'Số điện thoại — không bắt buộc, bạn có thể để trống', en: 'Phone number — optional, you may leave it blank' },
        { vi: 'Mật khẩu, lưu dưới dạng băm; chúng tôi không đọc được mật khẩu của bạn', en: 'Password, stored hashed; we cannot read your password' },
        { vi: 'Ngôn ngữ hiển thị bạn chọn', en: 'Your chosen display language' },
      ],
    },
    {
      heading: { vi: '2. Dữ liệu bạn tạo trong khi dùng', en: '2. Data you create while using the app' },
      bullets: [
        { vi: 'Bể cá: tên, kích thước, thể tích, loại nước, phong cách bố cục', en: 'Aquariums: name, dimensions, volume, water type, layout style' },
        { vi: 'Các loài cá trong từng bể và số lượng', en: 'Species in each tank and their quantity' },
        { vi: 'Ghi chép thông số nước theo thời gian', en: 'Water parameter logs over time' },
        { vi: 'Lịch nhắc (thay nước, vệ sinh lọc) và ngày đến hạn', en: 'Reminders (water change, filter cleaning) and due dates' },
        { vi: 'Ảnh bể bạn tải lên', en: 'Tank photos you upload' },
        { vi: 'Loài yêu thích và các loài bạn đã xem gần đây', en: 'Favorite species and species you viewed recently' },
        { vi: 'Đóng góp cộng đồng: loài mới, tên bản ngữ — kèm định danh người đóng góp để chúng tôi ghi nhận và duyệt', en: 'Community contributions: new species, local names — with a contributor reference so we can credit and review them' },
      ],
    },
    {
      heading: { vi: '3. Nội dung bạn chủ động công khai', en: '3. Content you choose to make public' },
      paragraphs: [
        {
          vi: 'Hai tính năng biến nội dung riêng thành công khai, và cả hai đều **chỉ chạy khi bạn tự bấm**:',
          en: 'Two features turn private content public, and both **only run when you click**:',
        },
        {
          vi: 'Đăng bể — sinh một trang công khai có ảnh, danh sách loài và số lượt thích. Bất kỳ ai có đường liên kết đều xem được. Bạn thu hồi được lúc nào cũng được, nhưng số lượt thích sẽ mất nếu đăng lại.',
          en: 'Publishing a tank — creates a public page with photos, species list and like count. Anyone with the link can view it. You can unpublish at any time, but likes are lost if you republish.',
        },
        {
          vi: 'Nộp bài thi — video của bạn được tải lên kênh YouTube của FishLover. Ban đầu ở chế độ không công khai để ban tổ chức duyệt; duyệt xong thì thành công khai trên YouTube. Nếu bị từ chối, video bị xoá khỏi YouTube và không phục hồi được.',
          en: 'Submitting a contest entry — your video is uploaded to the FishLover YouTube channel. It starts unlisted for organizer review; once approved it becomes public on YouTube. If rejected, the video is deleted from YouTube and cannot be restored.',
        },
      ],
    },
    {
      heading: { vi: '4. Điều chúng tôi cố ý KHÔNG lưu', en: '4. What we deliberately do NOT store' },
      paragraphs: [
        {
          vi: 'Trang "Cá cảnh theo quốc gia" được xây từ dữ liệu bể người dùng: nếu một bể ở nước X có loài Z thì hệ thống suy ra nước X có bán loài Z. Nhưng dữ liệu đẩy sang chỉ gồm **cặp mã quốc gia và mã loài** — không kèm định danh người dùng, không kèm định danh bể.',
          en: 'The "Fish by country" page is built from user tank data: if a tank in country X holds species Z, the system infers that country X sells species Z. But only the **country code and species code pair** is transferred — no user reference, no tank reference.',
        },
        {
          vi: 'Đây là lựa chọn thiết kế một chiều và có chủ đích: không ai truy ngược được từ danh sách quốc gia về việc ai đang nuôi con cá nào.',
          en: 'This is a deliberate one-way design: nobody can trace the country list back to who keeps which fish.',
        },
      ],
    },
    {
      heading: { vi: '5. Dịch vụ bên thứ ba', en: '5. Third-party services' },
      paragraphs: [
        {
          vi: 'Để hệ thống chạy được, một phần dữ liệu đi qua các dịch vụ sau. Mỗi dịch vụ chỉ nhận đúng phần cần thiết:',
          en: 'To operate, some data passes through the services below. Each receives only what it needs:',
        },
      ],
      bullets: [
        { vi: 'Cloudflare R2 — lưu ảnh loài, ảnh bể, và tệp video trước khi tải lên YouTube', en: 'Cloudflare R2 — stores species images, tank photos, and video files before YouTube upload' },
        { vi: 'Cloudflare Workers — phục vụ giao diện web', en: 'Cloudflare Workers — serves the web interface' },
        { vi: 'Resend — gửi email xác thực và đặt lại mật khẩu; nhận địa chỉ email của bạn', en: 'Resend — sends verification and password-reset email; receives your email address' },
        { vi: 'YouTube Data API (Google) — nhận video bài thi và trả về số lượt xem', en: 'YouTube Data API (Google) — receives contest videos and returns view counts' },
        { vi: 'Oracle Cloud — máy chủ đặt ứng dụng và cơ sở dữ liệu', en: 'Oracle Cloud — hosts the application servers and database' },
        { vi: 'DiceBear — sinh ảnh đại diện mặc định', en: 'DiceBear — generates default avatar images' },
      ],
    },
    {
      heading: { vi: '6. Nhật ký kỹ thuật', en: '6. Technical logs' },
      paragraphs: [
        {
          vi: 'Chúng tôi ghi nhật ký lỗi và hiệu năng để tìm nguyên nhân sự cố. Nhật ký có thể chứa thời điểm, đường dẫn được gọi, mã định danh yêu cầu và thông tin kỹ thuật do máy chủ web ghi lại. Nhật ký được lưu trên hạ tầng của chúng tôi, không bán và không chia sẻ cho mục đích quảng cáo.',
          en: 'We record error and performance logs to diagnose problems. Logs may contain timestamps, requested paths, request identifiers and technical information recorded by the web server. Logs are kept on our own infrastructure, never sold and never shared for advertising.',
        },
        {
          vi: 'Chúng tôi không dùng dịch vụ theo dõi hành vi hay quảng cáo của bên thứ ba.',
          en: 'We do not use third-party behavioural tracking or advertising services.',
        },
      ],
    },
    {
      heading: { vi: '7. Thông báo đẩy', en: '7. Push notifications' },
      paragraphs: [
        {
          vi: 'Nếu bạn bật thông báo, trình duyệt cấp cho chúng tôi một địa chỉ đăng ký để gửi nhắc lịch chăm bể. Bạn tắt được bất kỳ lúc nào trong phần Hồ sơ, hoặc trong cài đặt trình duyệt.',
          en: 'If you enable notifications, your browser gives us a subscription endpoint so we can send tank-care reminders. You can turn this off at any time in Profile, or in your browser settings.',
        },
      ],
    },
    {
      heading: { vi: '8. Quyền của bạn', en: '8. Your rights' },
      bullets: [
        { vi: 'Xem và sửa dữ liệu tài khoản trong phần Hồ sơ', en: 'View and edit your account data in Profile' },
        { vi: 'Xoá bể, ảnh, lịch nhắc và đóng góp của bạn ngay trong ứng dụng', en: 'Delete your tanks, photos, reminders and contributions directly in the app' },
        { vi: 'Thu hồi bể đã đăng, khiến trang công khai không còn truy cập được', en: 'Unpublish a published tank, making its public page inaccessible' },
        { vi: `Yêu cầu xoá toàn bộ tài khoản bằng cách gửi thư tới ${LIEN_HE}`, en: `Request full account deletion by writing to ${LIEN_HE}` },
      ],
    },
    {
      heading: { vi: '9. Dữ liệu loài cá', en: '9. Fish species data' },
      paragraphs: [
        {
          vi: 'Dữ liệu khoa học về loài trong FishLover có nguồn từ FishBase (www.fishbase.org). Đây là dữ liệu tham khảo, không phải lời khuyên thú y. Số liệu như khoảng nhiệt độ là khoảng sinh tồn ngoài tự nhiên, không phải khuyến nghị nuôi.',
          en: 'Scientific species data in FishLover comes from FishBase (www.fishbase.org). It is reference data, not veterinary advice. Figures such as temperature ranges describe survival in the wild, not recommended aquarium conditions.',
        },
      ],
    },
    {
      heading: { vi: '10. Thay đổi và liên hệ', en: '10. Changes and contact' },
      paragraphs: [
        {
          vi: `Khi chính sách này thay đổi, ngày cập nhật ở đầu trang sẽ đổi theo. Có câu hỏi hoặc yêu cầu về dữ liệu, gửi thư tới ${LIEN_HE}.`,
          en: `When this policy changes, the date at the top of this page changes with it. For questions or data requests, write to ${LIEN_HE}.`,
        },
      ],
    },
  ],
};

export const TERMS: LegalDoc = {
  title: { vi: 'Điều khoản sử dụng', en: 'Terms of Service' },
  updated: '2026-08-23',
  intro: {
    vi: 'FishLover là dự án cá nhân, làm vì yêu thích cá cảnh. Dùng dịch vụ nghĩa là bạn đồng ý với những điều dưới đây.',
    en: 'FishLover is a personal project built out of love for the aquarium hobby. Using the service means you agree to the terms below.',
  },
  sections: [
    {
      heading: { vi: '1. Dịch vụ được cung cấp như hiện có', en: '1. Service provided as-is' },
      paragraphs: [
        {
          vi: 'Đây không phải dịch vụ thương mại và không có cam kết thời gian hoạt động. Hệ thống có thể tạm dừng để bảo trì, gặp lỗi, hoặc thay đổi tính năng mà không báo trước. Hãy tự giữ bản sao những nội dung quan trọng với bạn — nhất là ảnh bể.',
          en: 'This is not a commercial service and carries no uptime guarantee. The system may go down for maintenance, hit bugs, or change features without notice. Keep your own copies of anything important to you — especially tank photos.',
        },
      ],
    },
    {
      heading: { vi: '2. Tài khoản', en: '2. Accounts' },
      bullets: [
        { vi: 'Đăng ký theo hình thức mời; đừng chia sẻ mã lời mời cho người bạn không tin tưởng', en: 'Registration is invitation-only; do not share your invitation code with people you do not trust' },
        { vi: 'Một người một tài khoản, thông tin phải đúng', en: 'One account per person, with accurate information' },
        { vi: 'Bạn chịu trách nhiệm cho hoạt động dưới tài khoản của mình', en: 'You are responsible for activity under your account' },
      ],
    },
    {
      heading: { vi: '3. Nội dung của bạn', en: '3. Your content' },
      paragraphs: [
        {
          vi: 'Ảnh, video và ghi chép bạn tải lên vẫn thuộc về bạn. Khi bạn chủ động đăng bể hoặc nộp bài thi, bạn cho phép chúng tôi hiển thị nội dung đó trên trang công khai và trên kênh YouTube của FishLover, phục vụ đúng mục đích tính năng đó.',
          en: 'Photos, videos and notes you upload remain yours. When you choose to publish a tank or submit a contest entry, you grant us permission to display that content on public pages and on the FishLover YouTube channel, for the purpose of that feature only.',
        },
        {
          vi: 'Chỉ tải lên nội dung do bạn tạo hoặc bạn có quyền dùng.',
          en: 'Only upload content you created or have the right to use.',
        },
      ],
    },
    {
      heading: { vi: '4. Quy định về cuộc thi', en: '4. Contest rules' },
      bullets: [
        { vi: 'Video phải nằm ngang, dài 2–5 phút, thấy rõ toàn bể và các loài đang nuôi', en: 'Videos must be landscape, 2–5 minutes long, clearly showing the whole tank and the species kept' },
        { vi: 'Không dùng nhạc có bản quyền — YouTube có thể tắt tiếng hoặc ẩn video, ảnh hưởng trực tiếp tới thứ hạng theo lượt xem', en: 'No copyrighted music — YouTube may mute or hide the video, directly affecting your view-count ranking' },
        { vi: 'Xếp hạng theo số lượt xem trên YouTube; mọi hành vi tăng lượt xem giả sẽ bị loại', en: 'Ranking is by YouTube view count; any artificial view inflation leads to disqualification' },
        { vi: 'Ban tổ chức có quyền từ chối bài dự thi và sẽ nêu lý do; video bị từ chối bị xoá khỏi YouTube và không phục hồi được', en: 'Organizers may reject entries and will state the reason; rejected videos are deleted from YouTube and cannot be restored' },
        { vi: 'Giải thưởng và nhà tài trợ của từng cuộc thi được ghi tại trang cuộc thi đó', en: 'Prizes and sponsors for each contest are listed on that contest page' },
      ],
    },
    {
      heading: { vi: '5. Điều không được làm', en: '5. What you must not do' },
      bullets: [
        { vi: 'Đăng nội dung vi phạm pháp luật, xúc phạm hoặc quấy rối người khác', en: 'Post unlawful, abusive or harassing content' },
        { vi: 'Đăng nội dung xâm phạm bản quyền của người khác', en: 'Post content that infringes someone else\'s copyright' },
        { vi: 'Cố tình gây tải bất thường, dò lỗ hổng hoặc truy cập dữ liệu của người khác', en: 'Deliberately generate abnormal load, probe for vulnerabilities, or access other people\'s data' },
        { vi: 'Thu thập dữ liệu tự động ở quy mô lớn từ hệ thống', en: 'Scrape data from the system at scale' },
      ],
    },
    {
      heading: { vi: '6. Dữ liệu loài và nguồn gốc', en: '6. Species data and attribution' },
      paragraphs: [
        {
          vi: 'Dữ liệu khoa học về loài có nguồn từ FishBase (www.fishbase.org). Đây là thông tin tham khảo dùng cho mục đích tra cứu, không thay thế tư vấn của người có chuyên môn. Chúng tôi không bảo đảm dữ liệu luôn chính xác hoặc cập nhật.',
          en: 'Scientific species data comes from FishBase (www.fishbase.org). It is reference information for lookup purposes and does not replace advice from a qualified professional. We do not guarantee the data is always accurate or up to date.',
        },
        {
          vi: 'Tên bản ngữ và danh sách loài bán theo quốc gia một phần do cộng đồng đóng góp và có thể chưa được kiểm chứng đầy đủ.',
          en: 'Local names and per-country traded species lists are partly community-contributed and may not be fully verified.',
        },
      ],
    },
    {
      heading: { vi: '7. Chấm dứt', en: '7. Termination' },
      paragraphs: [
        {
          vi: 'Bạn xoá tài khoản lúc nào cũng được. Chúng tôi có thể tạm ngưng hoặc xoá tài khoản vi phạm các điều khoản này, và sẽ nêu lý do khi làm vậy.',
          en: 'You may delete your account at any time. We may suspend or remove accounts that violate these terms, and will state the reason when we do.',
        },
      ],
    },
    {
      heading: { vi: '8. Thay đổi và liên hệ', en: '8. Changes and contact' },
      paragraphs: [
        {
          vi: `Điều khoản có thể thay đổi; ngày cập nhật ở đầu trang sẽ đổi theo. Liên hệ: ${LIEN_HE}.`,
          en: `These terms may change; the date at the top of this page changes with them. Contact: ${LIEN_HE}.`,
        },
      ],
    },
  ],
};
