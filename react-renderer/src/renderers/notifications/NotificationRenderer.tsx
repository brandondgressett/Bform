import React, { useState } from 'react';
import classNames from 'classnames';
import { Notification, NotificationChannelType } from '../../types';
import { formatRelativeTime } from '../../utils/formatting';

interface NotificationRendererProps {
  notification: Notification;
  variant?: 'toast' | 'alert' | 'list';
  showActions?: boolean;
  onMarkRead?: (notification: Notification) => Promise<void>;
  onDismiss?: (notification: Notification) => Promise<void>;
  onAction?: (notification: Notification, action: string) => Promise<void>;
  className?: string;
}

/**
 * Renders a single notification with various display options
 */
export const NotificationRenderer: React.FC<NotificationRendererProps> = ({
  notification,
  variant = 'list',
  showActions = true,
  onMarkRead,
  onDismiss,
  onAction,
  className
}) => {
  const [isUpdating, setIsUpdating] = useState(false);

  const getIcon = () => {
    switch (notification.channelType) {
      case NotificationChannelType.Email:
        return 'bi-envelope';
      case NotificationChannelType.SMS:
        return 'bi-phone';
      case NotificationChannelType.Web:
        return 'bi-bell';
      case NotificationChannelType.InApp:
        return 'bi-app-indicator';
      default:
        return 'bi-info-circle';
    }
  };

  const getPriorityColor = () => {
    switch (notification.priority) {
      case 'high':
        return 'danger';
      case 'medium':
        return 'warning';
      case 'low':
        return 'info';
      default:
        return 'secondary';
    }
  };

  const handleMarkRead = async () => {
    if (onMarkRead && !notification.isRead) {
      setIsUpdating(true);
      try {
        await onMarkRead(notification);
      } finally {
        setIsUpdating(false);
      }
    }
  };

  const handleDismiss = async () => {
    if (onDismiss) {
      setIsUpdating(true);
      try {
        await onDismiss(notification);
      } finally {
        setIsUpdating(false);
      }
    }
  };

  const handleAction = async (action: string) => {
    if (onAction) {
      setIsUpdating(true);
      try {
        await onAction(notification, action);
      } finally {
        setIsUpdating(false);
      }
    }
  };

  const containerClasses = classNames(
    'bf-notification',
    `bf-notification-${variant}`,
    {
      'bf-notification-unread': !notification.isRead,
      'bf-notification-updating': isUpdating
    },
    className
  );

  // Toast variant
  if (variant === 'toast') {
    return (
      <div className={`toast ${containerClasses}`} role="alert">
        <div className="toast-header">
          <i className={`${getIcon()} me-2 text-${getPriorityColor()}`}></i>
          <strong className="me-auto">{notification.title}</strong>
          <small>{formatRelativeTime(notification.createdDate)}</small>
          {showActions && onDismiss && (
            <button
              type="button"
              className="btn-close"
              aria-label="Close"
              onClick={handleDismiss}
              disabled={isUpdating}
            />
          )}
        </div>
        <div className="toast-body">
          {notification.body}
          {notification.actions && notification.actions.length > 0 && (
            <div className="mt-2">
              {notification.actions.map((action, index) => (
                <button
                  key={index}
                  className="btn btn-sm btn-primary me-2"
                  onClick={() => handleAction(action.id)}
                  disabled={isUpdating}
                >
                  {action.label}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>
    );
  }

  // Alert variant
  if (variant === 'alert') {
    return (
      <div 
        className={`alert alert-${getPriorityColor()} ${containerClasses}`} 
        role="alert"
      >
        <div className="d-flex align-items-start">
          <i className={`${getIcon()} me-2 fs-5`}></i>
          <div className="flex-grow-1">
            <h6 className="alert-heading mb-1">{notification.title}</h6>
            <p className="mb-0">{notification.body}</p>
            <small className="text-muted d-block mt-1">
              {formatRelativeTime(notification.createdDate)}
            </small>
            {notification.actions && notification.actions.length > 0 && (
              <div className="mt-2">
                {notification.actions.map((action, index) => (
                  <button
                    key={index}
                    className={`btn btn-sm btn-${getPriorityColor()} me-2`}
                    onClick={() => handleAction(action.id)}
                    disabled={isUpdating}
                  >
                    {action.label}
                  </button>
                ))}
              </div>
            )}
          </div>
          {showActions && onDismiss && (
            <button
              type="button"
              className="btn-close"
              aria-label="Dismiss"
              onClick={handleDismiss}
              disabled={isUpdating}
            />
          )}
        </div>
      </div>
    );
  }

  // List variant (default)
  return (
    <div className={containerClasses}>
      <div className="d-flex align-items-start p-3">
        {/* Icon */}
        <div className="me-3">
          <span className={`badge bg-${getPriorityColor()} p-2`}>
            <i className={getIcon()}></i>
          </span>
        </div>

        {/* Content */}
        <div className="flex-grow-1">
          <div className="d-flex justify-content-between align-items-start mb-1">
            <h6 className="mb-0">
              {notification.title}
              {!notification.isRead && (
                <span className="badge bg-primary ms-2">New</span>
              )}
            </h6>
            <small className="text-muted">
              {formatRelativeTime(notification.createdDate)}
            </small>
          </div>
          
          <p className="mb-2 text-body-secondary">{notification.body}</p>
          
          {/* Metadata */}
          <div className="d-flex flex-wrap gap-2 mb-2">
            <small className="text-muted">
              <i className="bi bi-tag me-1"></i>
              {notification.channelType}
            </small>
            {notification.tags?.length > 0 && (
              <small className="text-muted">
                {notification.tags.map(tag => (
                  <span key={tag} className="badge bg-light text-dark me-1">
                    {tag}
                  </span>
                ))}
              </small>
            )}
          </div>

          {/* Actions */}
          {showActions && (
            <div className="d-flex gap-2">
              {!notification.isRead && onMarkRead && (
                <button
                  className="btn btn-sm btn-outline-primary"
                  onClick={handleMarkRead}
                  disabled={isUpdating}
                >
                  <i className="bi bi-check2"></i> Mark Read
                </button>
              )}
              {notification.actions?.map((action, index) => (
                <button
                  key={index}
                  className="btn btn-sm btn-primary"
                  onClick={() => handleAction(action.id)}
                  disabled={isUpdating}
                >
                  {action.label}
                </button>
              ))}
              {onDismiss && (
                <button
                  className="btn btn-sm btn-outline-secondary"
                  onClick={handleDismiss}
                  disabled={isUpdating}
                >
                  <i className="bi bi-x"></i> Dismiss
                </button>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

NotificationRenderer.displayName = 'NotificationRenderer';

// Notification Center Component
interface NotificationCenterProps {
  notifications: Notification[];
  showFilters?: boolean;
  showMarkAllRead?: boolean;
  onMarkRead?: (notification: Notification) => Promise<void>;
  onMarkAllRead?: () => Promise<void>;
  onDismiss?: (notification: Notification) => Promise<void>;
  onAction?: (notification: Notification, action: string) => Promise<void>;
  className?: string;
}

/**
 * Renders a notification center with filtering and bulk actions
 */
export const NotificationCenter: React.FC<NotificationCenterProps> = ({
  notifications,
  showFilters = true,
  showMarkAllRead = true,
  onMarkRead,
  onMarkAllRead,
  onDismiss,
  onAction,
  className
}) => {
  const [filter, setFilter] = useState<'all' | 'unread' | 'read'>('all');
  const [channelFilter, setChannelFilter] = useState<NotificationChannelType | 'all'>('all');

  const filteredNotifications = notifications.filter(n => {
    if (filter === 'unread' && n.isRead) return false;
    if (filter === 'read' && !n.isRead) return false;
    if (channelFilter !== 'all' && n.channelType !== channelFilter) return false;
    return true;
  });

  const unreadCount = notifications.filter(n => !n.isRead).length;

  return (
    <div className={classNames('bf-notification-center', className)}>
      {/* Header */}
      <div className="bf-notification-header mb-3">
        <div className="d-flex justify-content-between align-items-center">
          <h5 className="mb-0">
            Notifications
            {unreadCount > 0 && (
              <span className="badge bg-primary ms-2">{unreadCount}</span>
            )}
          </h5>
          {showMarkAllRead && unreadCount > 0 && onMarkAllRead && (
            <button
              className="btn btn-sm btn-outline-primary"
              onClick={onMarkAllRead}
            >
              Mark All Read
            </button>
          )}
        </div>
      </div>

      {/* Filters */}
      {showFilters && (
        <div className="bf-notification-filters mb-3">
          <div className="btn-group btn-group-sm me-2" role="group">
            <button
              className={classNames('btn', {
                'btn-primary': filter === 'all',
                'btn-outline-primary': filter !== 'all'
              })}
              onClick={() => setFilter('all')}
            >
              All
            </button>
            <button
              className={classNames('btn', {
                'btn-primary': filter === 'unread',
                'btn-outline-primary': filter !== 'unread'
              })}
              onClick={() => setFilter('unread')}
            >
              Unread
            </button>
            <button
              className={classNames('btn', {
                'btn-primary': filter === 'read',
                'btn-outline-primary': filter !== 'read'
              })}
              onClick={() => setFilter('read')}
            >
              Read
            </button>
          </div>

          <select
            className="form-select form-select-sm"
            style={{ width: 'auto' }}
            value={channelFilter}
            onChange={(e) => setChannelFilter(e.target.value as any)}
          >
            <option value="all">All Channels</option>
            <option value={NotificationChannelType.Email}>Email</option>
            <option value={NotificationChannelType.SMS}>SMS</option>
            <option value={NotificationChannelType.Web}>Web</option>
            <option value={NotificationChannelType.InApp}>In-App</option>
          </select>
        </div>
      )}

      {/* Notifications List */}
      <div className="bf-notification-list">
        {filteredNotifications.length === 0 ? (
          <p className="text-muted text-center py-5">
            No notifications to display
          </p>
        ) : (
          <div className="list-group">
            {filteredNotifications.map(notification => (
              <div key={notification.id} className="list-group-item p-0">
                <NotificationRenderer
                  notification={notification}
                  variant="list"
                  onMarkRead={onMarkRead}
                  onDismiss={onDismiss}
                  onAction={onAction}
                />
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Styles */}
      <style>{`
        .bf-notification-unread {
          background-color: #f0f8ff;
        }
        .bf-notification-unread:hover {
          background-color: #e6f2ff;
        }
        .bf-notification-updating {
          opacity: 0.6;
          pointer-events: none;
        }
        .bf-notification-list .list-group-item {
          border: 1px solid rgba(0,0,0,.125);
          margin-bottom: -1px;
        }
        .bf-notification-list .list-group-item:first-child {
          border-top-left-radius: 0.375rem;
          border-top-right-radius: 0.375rem;
        }
        .bf-notification-list .list-group-item:last-child {
          border-bottom-left-radius: 0.375rem;
          border-bottom-right-radius: 0.375rem;
        }
      `}</style>
    </div>
  );
};

NotificationCenter.displayName = 'NotificationCenter';