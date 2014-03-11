var App = Ember.Application.create();

App.IndexRoute = Ember.Route.extend({
    model: function() {
        return $.getJSON('/tenders').then(function (data){
            return data.tenders;
        });
    }
});