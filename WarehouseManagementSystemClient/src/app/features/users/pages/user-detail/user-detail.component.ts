import { Component, OnInit } from '@angular/core';
import { PageBreadcrumbComponent } from "../../../../shared/components/common/page-breadcrumb/page-breadcrumb.component";
import { ComponentCardComponent } from "../../../../shared/components/common/component-card/component-card.component";
import { LabelComponent } from "../../../../shared/components/form/label/label.component";
import { InputDetailComponent } from "../../../../shared/components/form/input/input-detail.component";
import { DetailActionsComponent } from "../../../../shared/components/form/detail-actions/detail-actions.component";
import { User } from '../../model/user';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService } from '../../../services/user-service';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [PageBreadcrumbComponent, ComponentCardComponent, LabelComponent, InputDetailComponent, DetailActionsComponent],
  templateUrl: './user-detail.component.html'
})
export class UserDetailComponent implements OnInit {

  user!: User;
  id!: string;

  constructor(private router: Router, private activatedRoute: ActivatedRoute, private userService: UserService) { }
  ngOnInit(): void {
    this.id = this.activatedRoute.snapshot.paramMap.get('id')!;
    this.user = this.userService.getUser(this.id)!;
  }

  onEdit() {
    this.router.navigateByUrl(`/users/form/${(this.user as User).id}`)
  }
  onBack() {
    this.router.navigateByUrl('/users')
  }

}
