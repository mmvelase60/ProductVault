import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type NotificationKind = 'error' | 'info';

export interface Notification {
  id: number;
  kind: NotificationKind;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly subject = new BehaviorSubject<Notification[]>([]);
  private nextId = 1;
  readonly notifications$ = this.subject.asObservable();

  show(message: string, kind: NotificationKind = 'info'): void {
    const notification = { id: this.nextId++, kind, message };
    this.subject.next([...this.subject.value, notification]);
    window.setTimeout(() => this.dismiss(notification.id), 7000);
  }

  dismiss(id: number): void {
    this.subject.next(this.subject.value.filter(notification => notification.id !== id));
  }
}
