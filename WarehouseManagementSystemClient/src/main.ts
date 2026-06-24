import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import 'zone.js';


bootstrapApplication(AppComponent, appConfig)
  .catch((err: unknown) => {
    setTimeout(() => {
      throw err;
    });
  });


// komenda do uruchomienia projektu: ng serve --ssl true --ssl-cert src/localhost.crt --ssl-key src/localhost.key
