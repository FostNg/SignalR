# Emoji Reactions Feature - Implementation Plan

## Context
- **Project:** ChatBox - Local LAN chat application with WPF (.NET), MVVM-light, PostgreSQL
- **Feature:** Add emoji reactions to messages (like Discord)
- **User Requirements:**
  - Emoji picker popup on right-click/long-press
  - Heart emoji with no color when not hovered, colored when hovered
  - Show 5-6 emoji options
  - Persistent storage in PostgreSQL + real-time broadcast to all users
  - Show reactions on messages (who reacted)

## Architecture Overview

```
Client (WPF)                          Server (WPF + Console)
    |                                      |
    |--- TCP 9999 (Chat) ----------------> |
    |<--- Broadcast/Messages -------------- |
    |                                      |
    |--------- REACT|<msgId>|<emoji>------> |
    |<------- REACTION_UPDATE ------------ |
    |                                      |
 PostgreSQL Database
 (Stores Users, ChatMessages, Reactions)
```

## Message Protocol Extensions

### New Messages
```
REACT|MessageId|Emoji
REACTION_UPDATE|MessageId|ReactionsJson
```

### ReactionsJson Format
```json
[{"emoji":"👍","users":["user1","user2"]},{"emoji":"❤️","users":["user3"]}]
```

## Database Schema

### New Table: MessageReaction
```sql
CREATE TABLE message_reactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    message_id UUID NOT NULL REFERENCES chat_messages(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    emoji VARCHAR(50) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(message_id, user_id, emoji)
);

CREATE INDEX idx_message_reactions_message_id ON message_reactions(message_id);
```

## Tasks

### Task 1: Database Schema Update
- **Files:** `LocalChat.Core\Data\ChatDbContext.cs`
- Add `MessageReaction` entity
- Add `Reactions` navigation property to `ChatMessage`
- Add unique constraint (message_id, user_id, emoji)
- Run migration or ensure table creation

### Task 2: Message Model Update
- **Files:** `LocalChat.Core\Models\ChatMessage.cs`
- Add `Reactions` property (list of MessageReaction)
- Add `ReactionSummary` computed property for UI display

### Task 3: Server - Reaction Handling
- **Files:** `LocalChat.Core\Services\ChatService.cs`
- Add `ProcessReactionAsync` method
- Add `BroadcastReactionUpdate` method
- Parse `REACT|MessageId|Emoji` message
- Store reaction in database
- Broadcast to all clients

### Task 4: Client - Reaction Popup UI
- **Files:** `ChatBox.Client\MainWindow.xaml`
- Create `ReactionPopup` custom control or Popup
- 5-6 emoji options: 👍 👎 😂 ❤️ 😮 😢
- Heart emoji with hover effect (no color → colored)
- Position popup near the message

### Task 5: Client - Right-Click Handler
- **Files:** `ChatBox.Client\MainWindow.xaml.cs`
- Add `Message_MouseRightButtonDown` handler
- Show emoji popup at cursor position
- Handle emoji selection
- Send `REACT` message to server

### Task 6: Client - Display Reactions on Messages
- **Files:** `ChatBox.Client\MainWindow.xaml`
- Add reactions display below message content
- Show emoji + count + user names tooltip
- Update UI when `REACTION_UPDATE` received
- Refresh message list

### Task 7: Client - Send Reaction
- **Files:** `ChatBox.Client\MainWindow.xaml.cs`
- Parse and handle `REACTION_UPDATE` messages
- Update local ChatMessage with reactions
- Refresh message list

## Dependencies
- Task 1 → Task 3 (server needs database)
- Task 1 → Task 2 (model needs entity)
- Task 2, 3 → Task 7 (client needs server)
- Task 5, 7 → Task 6 (popup and receiving need display)

## Status
- [ ] Task 1: Database Schema Update
- [ ] Task 2: Message Model Update
- [ ] Task 3: Server - Reaction Handling
- [ ] Task 4: Client - Reaction Popup UI
- [ ] Task 5: Client - Right-Click Handler
- [ ] Task 6: Client - Display Reactions on Messages
- [ ] Task 7: Client - Send Reaction

## Next Steps
1. Create worktree for feature branch
2. Execute tasks sequentially using subagent-driven development
3. Each task: implement → spec review → code quality review → commit