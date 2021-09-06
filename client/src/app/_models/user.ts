import { Friend } from './Friend';

export interface User {
	username: string;
	token: string;
	photoUrl: string;
	name: string;
	gender: string;
	friends: Friend[];
}
