import { Component } from '@angular/core';
import { Routes } from '@angular/router';
@Component({ template: '', standalone: true })
class RouteMarker {}
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'overview' },
  ...[
    'overview',
    'map',
    'topology',
    'meters',
    'profiles',
    'gateways',
    'alerts',
    'incidents',
    'copilot',
    'reports',
    'simulator',
    'settings',
  ].map((path) => ({ path, component: RouteMarker })),
  { path: '**', redirectTo: 'overview' },
];
