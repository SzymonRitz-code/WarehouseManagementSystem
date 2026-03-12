import { Component, signal } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'wms-root',
  standalone:true,
  imports: [RouterModule],
  templateUrl: './app.component.html',
  styleUrl: './app.css'
})
export class AppComponent {
  protected readonly title = signal('WarehouseManagementSystemClient');
}


// @Component({
//   selector: 'app-root',
//   standalone: true,
//   imports: [
//     RouterModule,
//   ],
//   templateUrl: './app.component.html',
//   styleUrl: './app.component.css',
// })
// export class AppComponent {
//   title = 'Angular Ecommerce Dashboard | TailAdmin';
// }
