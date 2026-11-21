/** Định dạng ảnh BE nhận cho upload qua presigned PUT (ảnh bể, ảnh loài cộng đồng). */
export const UPLOAD_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

/** Trần dung lượng ảnh upload — BE kiểm tra lại bằng HEAD ở bước confirm, đây chỉ để người dùng không chạm lỗi. */
export const UPLOAD_IMAGE_MAX_BYTES = 1 * 1024 * 1024;

/**
 * Nén ảnh phía client cho tới khi ≤ maxBytes: thu về rộng tối đa 1920px, giảm dần chất lượng, cuối cùng giảm nửa kích thước.
 * PNG không nén có mất mát được — chỉ resize, nên vẫn có thể vượt trần; caller phải kiểm tra `blob.size`.
 */
export async function compressImage(file: File, maxBytes = UPLOAD_IMAGE_MAX_BYTES): Promise<Blob> {
  const bitmap = await createImageBitmap(file);
  const { width, height } = bitmap;

  const scale = width > 1920 ? 1920 / width : 1;
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
