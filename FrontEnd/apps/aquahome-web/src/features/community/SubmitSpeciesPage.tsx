import { useNavigate } from 'react-router-dom';
import SubmitCommunitySpeciesModal from './SubmitCommunitySpeciesModal';

/** Trang riêng cho sidebar — mở modal gửi loài lai tạo ngay khi vào, đóng thì quay lại trang trước. */
export default function SubmitSpeciesPage() {
  const navigate = useNavigate();
  return <SubmitCommunitySpeciesModal onClose={() => navigate(-1)} />;
}
