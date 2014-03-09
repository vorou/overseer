var App = Ember.Application.create();

//App.Router.map(function () {
//    this.resource('todos', { path: '/' });
//});

App.IndexRoute = Ember.Route.extend({
    model: function() {
        return tenders;
    }
});

var tenders = [
{
    price: "1 999",
    url: "www.ya.ru",
    name: "Hello!",
    regionName: "Region"
}]