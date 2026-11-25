/** Định dạng ảnh BE nhận cho upload qua presigned PUT (ảnh bể, ảnh loài cộng đồng). */
export const UPLOAD_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

/**
 * Trần dung lượng ảnh loài cộng đồng — BE kiểm tra lại bằng HEAD ở bước confirm, đây chỉ để người dùng không chạm lỗi.
 * Mỗi nghiệp vụ có trần riêng ở BE: ảnh bể và logo contest dùng hằng bên dưới, KHÔNG dùng chung hằng này.
 */
export const UPLOAD_IMAGE_MAX_BYTES = 1 * 1024 * 1024;

/** Trần ảnh bể (BE: AquariumMediaService.MaxUploadBytes). */
export const AQUARIUM_IMAGE_MAX_BYTES = 1.5 * 1024 * 1024;

/** Logo sponsor / ảnh hạng giải: nén hướng tới ~200 KB, BE từ chối trên 300 KB (ContestService.MaxLogoUploadBytes). */
export const LOGO_IMAGE_TARGET_BYTES = 200 * 1024;
export const LOGO_IMAGE_MAX_BYTES = 300 * 1024;
const LOGO_MAX_WIDTH = 512;

/**
 * Chuẩn bị logo/ảnh giải để upload: SVG và file đã nhỏ giữ nguyên, còn lại nén về ~200 KB, rộng tối đa 512px.
 * Caller phải kiểm tra `blob.size > LOGO_IMAGE_MAX_BYTES` (PNG chỉ resize được, vẫn có thể vượt trần).
 */
export async function prepareLogoImage(file: File): Promise<Blob> {
  if (file.type === 'image/svg+xml' || file.size <= LOGO_IMAGE_TARGET_BYTES) return file;
  return compressImage(file, LOGO_IMAGE_TARGET_BYTES, LOGO_MAX_WIDTH);
}

/**
 * Nén ảnh phía client cho tới khi ≤ maxBytes: thu về rộng tối đa maxWidth (mặc định 1920px), giảm dần chất lượng,
 * cuối cùng giảm nửa kích thước.
 * PNG không nén có mất mát được — chỉ resize, nên vẫn có thể vượt trần; caller phải kiểm tra `blob.size`.
 */
export async function compressImage(file: File, maxBytes = UPLOAD_IMAGE_MAX_BYTES, maxWidth = 1920): Promise<Blob> {
  const bitmap = await createImageBitmap(file);
  const { width, height } = bitmap;

  const scale = width > maxWidth ? maxWidth / width : 1;
  const canvasW = Math.round(width * scale);
  const canvasH = Math.round(height * scale);

  const canvas = document.createElement('canvas');
  canvas.width = canvasW;
  canvas.height = canvasH;
  const ctx = canvas.getContext('2d')!;
  ctx.drawImage(bitmap, 0, 0, canvasW, canvasH);

  const toBlob = (type: string, quality?: number) =>
    new Promise<Blob>((res, rej) => canvas.toBlob(b => (b ? res(b) : rej(new Error('canvas.toBlob failed'))), type, quality));

  const mimeType = file.type === 'image/png' ? 'image/png' : file.type === 'image/webp' ? 'image/webp' : 'image/jpeg';
  if (mimeType === 'image/png') {
    return toBlob('image/png');
  }

  for (const quality of [0.9, 0.8, 0.7, 0.6, 0.5, 0.4]) {
    const blob = await toBlob(mimeType, quality);
    if (blob.size <= maxBytes) return blob;
  }

  canvas.width = Math.round(canvasW / 2);
  canvas.height = Math.round(canvasH / 2);
  ctx.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
  return toBlob(mimeType, 0.6);
}
