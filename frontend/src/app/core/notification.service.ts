import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type NotificationKind = 'error' | 'info' | 'success';

export interface Notification {
  id: number;
  kind: NotificationKind;
  message: string;
}

export interface MessageDialog {
  kind: NotificationKind;
  title: string;
  message: string;
  actionLabel: string;
  onClose?: () => void;
}

export interface ConfirmationDialog {
  title: string;
  message: string;
  confirmLabel: string;
  cancelLabel?: string;
  onConfirm: () => void;
  onCancel?: () => void;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly subject = new BehaviorSubject<Notification[]>([]);
  private readonly dialogSubject = new BehaviorSubject<MessageDialog | null>(null);
  private readonly confirmationSubject = new BehaviorSubject<ConfirmationDialog | null>(null);
  private nextId = 1;
  readonly notifications$ = this.subject.asObservable();
  readonly dialog$ = this.dialogSubject.asObservable();
  readonly confirmation$ = this.confirmationSubject.asObservable();

  show(message: string, kind: NotificationKind = 'info'): void {
    const notification = { id: this.nextId++, kind, message };
    this.subject.next([...this.subject.value, notification]);
    window.setTimeout(() => this.dismiss(notification.id), 7000);
  }

  dismiss(id: number): void {
    this.subject.next(this.subject.value.filter(notification => notification.id !== id));
  }

  showDialog(dialog: Omit<MessageDialog, 'actionLabel'> & { actionLabel?: string }): void {
    this.dialogSubject.next({ ...dialog, actionLabel: dialog.actionLabel ?? 'Close' });
  }

  dismissDialog(): void {
    const dialog = this.dialogSubject.value;
    this.dialogSubject.next(null);
    dialog?.onClose?.();
  }

  showConfirmation(dialog: ConfirmationDialog): void {
    this.confirmationSubject.next({ ...dialog, cancelLabel: dialog.cancelLabel ?? 'Cancel' });
  }

  confirm(): void {
    const confirmation = this.confirmationSubject.value;
    this.confirmationSubject.next(null);
    confirmation?.onConfirm();
  }

  dismissConfirmation(): void {
    const confirmation = this.confirmationSubject.value;
    this.confirmationSubject.next(null);
    confirmation?.onCancel?.();
  }
}
