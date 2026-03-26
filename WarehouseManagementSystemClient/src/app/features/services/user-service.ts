import { Injectable } from '@angular/core';
import { CreateUser } from '../users/model/create-user';
import { User } from '../users/model/user';

@Injectable({
  providedIn: 'root',
})
export class UserService {

  users: User[] = [];
  addUser(user: CreateUser) {
    const newId = this.users.length > 0
      ? Math.max(...this.users.map(p => Number(p.id))) + 1
      : 0;
    const userToAdd: User = {
      ...user,
      id: newId.toString(),
      createdAt: new Date()
    }
    this.users.push(userToAdd);
    return userToAdd;
  }
  getUser(id: string) {
    return this.users.find(u => u.id === id)
  }

}
