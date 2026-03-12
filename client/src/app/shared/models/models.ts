export interface User {
  id: string;
  email: string;
  displayName: string;
  role: string;
  avatarUrl?: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface RegisterRequest {
  email: string;
  displayName: string;
  password: string;
  confirmPassword: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface Meeting {
  id: string;
  title: string;
  meetingCode: string;
  hostId: string;
  hostName: string;
  scheduledAt?: string;
  createdAt: string;
  status: string;
  hasWaitingRoom: boolean;
  isPasswordProtected: boolean;
  participantCount: number;
}

export interface CreateMeetingRequest {
  title: string;
  scheduledAt?: string;
  password?: string;
  hasWaitingRoom: boolean;
}

export interface JoinMeetingRequest {
  meetingCode: string;
  password?: string;
}

export interface Participant {
  id: string;
  userId: string;
  displayName: string;
  avatarUrl?: string;
  isAudioOn: boolean;
  isVideoOn: boolean;
  role: string;
  joinedAt: string;
}

export interface ChatMessage {
  id: string;
  senderId: string;
  senderName: string;
  content: string;
  sentAt: string;
}
