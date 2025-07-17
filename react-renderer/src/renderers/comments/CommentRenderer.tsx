import React, { useState } from 'react';
import classNames from 'classnames';
import { Comment } from '../../types';
import { formatRelativeTime } from '../../utils/formatting';

interface CommentRendererProps {
  comment: Comment;
  currentUserId?: string;
  allowEdit?: boolean;
  allowDelete?: boolean;
  onEdit?: (commentId: string, newText: string) => Promise<void>;
  onDelete?: (commentId: string) => Promise<void>;
  onReply?: (parentId: string, text: string) => Promise<void>;
  className?: string;
}

/**
 * Renders a single comment with nested replies
 */
export const CommentRenderer: React.FC<CommentRendererProps> = ({
  comment,
  currentUserId,
  allowEdit = false,
  allowDelete = false,
  onEdit,
  onDelete,
  onReply,
  className
}) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editText, setEditText] = useState(comment.text);
  const [isReplying, setIsReplying] = useState(false);
  const [replyText, setReplyText] = useState('');
  const [isDeleting, setIsDeleting] = useState(false);

  const isOwner = currentUserId && comment.userId === currentUserId;
  const canEdit = allowEdit && isOwner && onEdit;
  const canDelete = allowDelete && isOwner && onDelete;
  const canReply = !!onReply;

  const handleSaveEdit = async () => {
    if (onEdit && editText.trim()) {
      await onEdit(comment.id, editText.trim());
      setIsEditing(false);
    }
  };

  const handleDelete = async () => {
    if (onDelete && window.confirm('Are you sure you want to delete this comment?')) {
      setIsDeleting(true);
      await onDelete(comment.id);
    }
  };

  const handleSaveReply = async () => {
    if (onReply && replyText.trim()) {
      await onReply(comment.id, replyText.trim());
      setReplyText('');
      setIsReplying(false);
    }
  };

  const containerClasses = classNames(
    'bf-comment',
    {
      'bf-comment-deleted': comment.isDeleted,
      'bf-comment-editing': isEditing,
      'bf-comment-deleting': isDeleting
    },
    className
  );

  return (
    <div className={containerClasses}>
      <div className="d-flex">
        {/* Avatar */}
        <div className="bf-comment-avatar me-3">
          <div className="rounded-circle bg-secondary text-white d-flex align-items-center justify-content-center" 
               style={{ width: '40px', height: '40px' }}>
            {comment.userName?.charAt(0).toUpperCase() || '?'}
          </div>
        </div>

        {/* Content */}
        <div className="flex-grow-1">
          {/* Header */}
          <div className="bf-comment-header mb-1">
            <strong className="me-2">{comment.userName || 'Anonymous'}</strong>
            <small className="text-muted">
              {formatRelativeTime(comment.createdDate)}
              {comment.modifiedDate && comment.modifiedDate !== comment.createdDate && (
                <span className="ms-1">(edited)</span>
              )}
            </small>
          </div>

          {/* Body */}
          {comment.isDeleted ? (
            <p className="text-muted fst-italic mb-2">
              [This comment has been deleted]
            </p>
          ) : isEditing ? (
            <div className="bf-comment-edit mb-2">
              <textarea
                className="form-control mb-2"
                value={editText}
                onChange={(e) => setEditText(e.target.value)}
                rows={3}
              />
              <div>
                <button 
                  className="btn btn-sm btn-primary me-2"
                  onClick={handleSaveEdit}
                  disabled={!editText.trim()}
                >
                  Save
                </button>
                <button 
                  className="btn btn-sm btn-secondary"
                  onClick={() => {
                    setIsEditing(false);
                    setEditText(comment.text);
                  }}
                >
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <div className="bf-comment-text mb-2">
              {comment.text.split('\n').map((line, i) => (
                <React.Fragment key={i}>
                  {line}
                  {i < comment.text.split('\n').length - 1 && <br />}
                </React.Fragment>
              ))}
            </div>
          )}

          {/* Actions */}
          {!comment.isDeleted && !isEditing && (
            <div className="bf-comment-actions mb-2">
              {canReply && (
                <button 
                  className="btn btn-sm btn-link text-decoration-none ps-0"
                  onClick={() => setIsReplying(!isReplying)}
                >
                  Reply
                </button>
              )}
              {canEdit && (
                <button 
                  className="btn btn-sm btn-link text-decoration-none"
                  onClick={() => setIsEditing(true)}
                >
                  Edit
                </button>
              )}
              {canDelete && (
                <button 
                  className="btn btn-sm btn-link text-decoration-none text-danger"
                  onClick={handleDelete}
                  disabled={isDeleting}
                >
                  Delete
                </button>
              )}
            </div>
          )}

          {/* Reply Form */}
          {isReplying && (
            <div className="bf-comment-reply mb-3">
              <textarea
                className="form-control mb-2"
                placeholder="Write a reply..."
                value={replyText}
                onChange={(e) => setReplyText(e.target.value)}
                rows={2}
              />
              <div>
                <button 
                  className="btn btn-sm btn-primary me-2"
                  onClick={handleSaveReply}
                  disabled={!replyText.trim()}
                >
                  Post Reply
                </button>
                <button 
                  className="btn btn-sm btn-secondary"
                  onClick={() => {
                    setIsReplying(false);
                    setReplyText('');
                  }}
                >
                  Cancel
                </button>
              </div>
            </div>
          )}

          {/* Nested Replies */}
          {comment.replies && comment.replies.length > 0 && (
            <div className="bf-comment-replies ms-4 mt-3">
              {comment.replies.map(reply => (
                <CommentRenderer
                  key={reply.id}
                  comment={reply}
                  currentUserId={currentUserId}
                  allowEdit={allowEdit}
                  allowDelete={allowDelete}
                  onEdit={onEdit}
                  onDelete={onDelete}
                  onReply={onReply}
                />
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

CommentRenderer.displayName = 'CommentRenderer';

// Comments Thread Component
interface CommentThreadProps {
  comments: Comment[];
  entityId: string;
  entityType: string;
  currentUserId?: string;
  allowAdd?: boolean;
  allowEdit?: boolean;
  allowDelete?: boolean;
  onAddComment?: (text: string) => Promise<void>;
  onEditComment?: (commentId: string, newText: string) => Promise<void>;
  onDeleteComment?: (commentId: string) => Promise<void>;
  onReplyComment?: (parentId: string, text: string) => Promise<void>;
  className?: string;
}

/**
 * Renders a complete comment thread with add functionality
 */
export const CommentThread: React.FC<CommentThreadProps> = ({
  comments,
  entityId,
  entityType,
  currentUserId,
  allowAdd = true,
  allowEdit = false,
  allowDelete = false,
  onAddComment,
  onEditComment,
  onDeleteComment,
  onReplyComment,
  className
}) => {
  const [newCommentText, setNewCommentText] = useState('');
  const [isAddingComment, setIsAddingComment] = useState(false);

  const handleAddComment = async () => {
    if (onAddComment && newCommentText.trim()) {
      setIsAddingComment(true);
      try {
        await onAddComment(newCommentText.trim());
        setNewCommentText('');
      } finally {
        setIsAddingComment(false);
      }
    }
  };

  // Get top-level comments (no parentId)
  const topLevelComments = comments.filter(c => !c.parentId);

  return (
    <div className={classNames('bf-comment-thread', className)}>
      {/* Add Comment Form */}
      {allowAdd && onAddComment && (
        <div className="bf-comment-add mb-4">
          <h5>Comments</h5>
          <textarea
            className="form-control mb-2"
            placeholder="Add a comment..."
            value={newCommentText}
            onChange={(e) => setNewCommentText(e.target.value)}
            rows={3}
          />
          <button 
            className="btn btn-primary"
            onClick={handleAddComment}
            disabled={!newCommentText.trim() || isAddingComment}
          >
            {isAddingComment ? 'Posting...' : 'Post Comment'}
          </button>
        </div>
      )}

      {/* Comments List */}
      <div className="bf-comments-list">
        {topLevelComments.length === 0 ? (
          <p className="text-muted">No comments yet. Be the first to comment!</p>
        ) : (
          topLevelComments.map(comment => (
            <div key={comment.id} className="mb-3">
              <CommentRenderer
                comment={comment}
                currentUserId={currentUserId}
                allowEdit={allowEdit}
                allowDelete={allowDelete}
                onEdit={onEditComment}
                onDelete={onDeleteComment}
                onReply={onReplyComment}
              />
            </div>
          ))
        )}
      </div>

      {/* Styles */}
      <style>{`
        .bf-comment {
          padding: 1rem;
          border-radius: 0.25rem;
          background-color: #f8f9fa;
        }
        .bf-comment:hover {
          background-color: #e9ecef;
        }
        .bf-comment-deleted {
          opacity: 0.6;
        }
        .bf-comment-deleting {
          opacity: 0.5;
          pointer-events: none;
        }
        .bf-comment-actions .btn-link {
          padding: 0.25rem 0.5rem;
          font-size: 0.875rem;
        }
        .bf-comment-replies {
          border-left: 2px solid #dee2e6;
          padding-left: 1rem;
        }
        .bf-comment-edit textarea,
        .bf-comment-reply textarea {
          resize: vertical;
        }
      `}</style>
    </div>
  );
};

CommentThread.displayName = 'CommentThread';