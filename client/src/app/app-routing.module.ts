import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardContentComponent } from './dashboard-content/dashboard-content.component';
import { FriendRequestsComponent } from './dashboard-content/discover/discover-left/friend-requests/friend-requests.component';
import { LoginComponent } from './login/login.component';
import { AuthGuard } from './_guards/auth.guard';
import { DiscoverComponent } from './dashboard-content/discover/discover.component';
import { MessagesLeftComponent } from './dashboard-content/messages/messages-left/messages-left.component';
import { MessagesComponent } from './dashboard-content/messages/messages.component';
import { combineAll } from 'rxjs/operators';
import { SettingsLeftComponent } from './dashboard-content/settings/settings-left/settings-left.component';
import { SettingsComponent } from './dashboard-content/settings/settings.component';

const routes: Routes = [
	{
		path: 'login',
		component: LoginComponent,
		data: { animationState: 'one' },
	},
	{
		path: '',
		component: DashboardContentComponent,
		canActivate: [AuthGuard],
		data: { animationState: 'two' },
		children: [
			{
				path: '',
				redirectTo: 'discover',
				pathMatch: 'full',
			},

			{
				path: 'discover',
				children: [
					{
						path: '',
						component: FriendRequestsComponent,
						canActivate: [AuthGuard],
						outlet: 'left',
					},
					{
						path: '',
						component: DiscoverComponent,
						canActivate: [AuthGuard],
						outlet: 'right',
					},
				],
			},
			{
				path: 'messages',
				children: [
					{
						path: '',
						component: MessagesLeftComponent,
						canActivate: [AuthGuard],
						outlet: 'left',
					},
					{
						path: '',
						component: MessagesComponent,
						canActivate: [AuthGuard],
						outlet: 'right',
					},
				],
			},
			{
				path: 'settings',
				children: [
					{
						path: '',
						component: SettingsLeftComponent,
						canActivate: [AuthGuard],
						outlet: 'left',
					},
					{
						path: '',
						component: SettingsComponent,
						canActivate: [AuthGuard],
						outlet: 'right',
					},
				],
			},
		],
	},
	{
		path: 'messages',
		component: DashboardContentComponent,
		pathMatch: 'full',
		children: [
			{
				path: '',
				component: MessagesLeftComponent,
				canActivate: [AuthGuard],
				outlet: 'left',
			},
			{
				path: '',
				component: MessagesComponent,
				canActivate: [AuthGuard],
				outlet: 'right',
			},
		],
	},
];

@NgModule({
	imports: [RouterModule.forRoot(routes)],
	exports: [RouterModule],
})
export class AppRoutingModule {}
