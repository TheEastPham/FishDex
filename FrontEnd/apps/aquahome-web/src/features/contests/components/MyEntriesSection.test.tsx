// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { ContestEntryStatus, confirmEntryUpload } from '@fishlover/shared';
import type { ContestEntryDto } from '@fishlover/shared';
import MyEntriesSection from './MyEntriesSection';

vi.mock('@fishlover/shared', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@fishlover/shared')>()),
  confirmEntryUpload: vi.fn(),
}));

// t giả trả lại chính key — test khẳng định đúng câu nào được chọn, không phụ thuộc bản dịch.
const t = ((key: string) => key) as unknown as Parameters<typeof MyEntriesSection>[0]['t'];

function entry(status: ContestEntryStatus, extra: Partial<ContestEntryDto> = {}): ContestEntryDto {
  return {
    id: `entry-${status}`,
    contestId: 'contest-1',
    aquariumSnapshotId: 'snap-1',
    youTubeVideoId: null,
    youTubeViewCount: 0,
    prizeTierId: null,
    prizeTierName: null,
    status,
    submittedAt: '2026-11-26T10:00:00Z',
    title: 'Bể của tôi',
    description: null,
    rejectionReason: null,
    aquariumName: null,
    ownerNickname: null,
    snapshotSlug: null,
    ...extra,
  } as ContestEntryDto;
}

function renderEntries(entries: ContestEntryDto[], onChanged = vi.fn()) {
  render(<MyEntriesSection contestId="contest-1" entries={entries} t={t} onChanged={onChanged} />);
  return onChanged;
}

afterEach(cleanup);

describe('MyEntriesSection', () => {
  it('shows a failed entry with its own label and reason, without a retry button', () => {
    renderEntries([entry(ContestEntryStatus.Failed, { rejectionReason: 'Không đăng được video lên YouTube.' })]);

    expect(screen.getByText('contests.stFailed')).toBeTruthy();
    expect(screen.getByText('Không đăng được video lên YouTube.')).toBeTruthy();
    expect(screen.queryByText('contests.retryConfirm')).toBeNull();
  });

  it('tells a queued entry it will be posted, and offers nothing to press', () => {
    renderEntries([entry(ContestEntryStatus.Queued)]);

    expect(screen.getByText('contests.stQueued')).toBeTruthy();
    expect(screen.getByText('contests.queuedHint')).toBeTruthy();
    expect(screen.queryByText('contests.retryConfirm')).toBeNull();
  });

  it('lets a pending entry be confirmed again and reloads the list afterwards', async () => {
    vi.mocked(confirmEntryUpload).mockResolvedValue(undefined);
    const onChanged = renderEntries([entry(ContestEntryStatus.Pending)]);

    fireEvent.click(screen.getByText('contests.retryConfirm'));

    await waitFor(() => expect(onChanged).toHaveBeenCalledOnce());
    expect(confirmEntryUpload).toHaveBeenCalledWith('contest-1', `entry-${ContestEntryStatus.Pending}`);
  });

  it('shows the backend reason when confirming again is refused', async () => {
    vi.mocked(confirmEntryUpload).mockRejectedValue({
      response: { status: 422, data: { message: 'Cuộc thi đã đóng, không nhận thêm bài.' } },
    });
    const onChanged = renderEntries([entry(ContestEntryStatus.Pending)]);

    fireEvent.click(screen.getByText('contests.retryConfirm'));

    expect(await screen.findByText('Cuộc thi đã đóng, không nhận thêm bài.')).toBeTruthy();
    expect(onChanged).toHaveBeenCalledOnce(); // bài có thể đã đổi trạng thái kể cả khi lỗi
  });

  it('links a published entry to its YouTube video', () => {
    renderEntries([entry(ContestEntryStatus.Published, { youTubeVideoId: 'abc123' })]);

    const link = screen.getByText('contests.watchVideo').closest('a');
    expect(link?.getAttribute('href')).toBe('https://www.youtube.com/watch?v=abc123');
  });

  it('shows the admin reason for a rejected entry', () => {
    renderEntries([entry(ContestEntryStatus.Rejected, { rejectionReason: 'Video không phải bể cá.' })]);

    expect(screen.getByText('contests.stRejected')).toBeTruthy();
    expect(screen.getByText('Video không phải bể cá.')).toBeTruthy();
  });
});
