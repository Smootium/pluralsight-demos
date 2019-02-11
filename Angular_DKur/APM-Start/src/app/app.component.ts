import { Component } from '@angular/core';
//the Decorator is a function so the ( ) are necessary
//we are passing an object to the Component function so the { } are necessary 
@Component({
  selector: 'pm-root',//'selector' defines a directive (name) so it can be used/referenced in HTML (a directive is a custom HTML tag)
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  pageTitle: string = 'Acme Product Management';
}
