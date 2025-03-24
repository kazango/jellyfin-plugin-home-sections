'use strict';

const HomeScreenSections = {
    init: function() {
        var MutationObserver = window.MutationObserver || window.WebKitMutationObserver;
        var myObserver = new MutationObserver(this.mutationHandler);
        var observerConfig = { childList: true, characterData: true, attributes: true, subtree: true };

        $("body").each(function () {
            myObserver.observe(this, observerConfig);
        });
    },
    mutationHandler: function(mutationRecords) {
        mutationRecords.forEach(function (mutation) {
            if (mutation.addedNodes && mutation.addedNodes.length > 0) {
                [].some.call(mutation.addedNodes, function(addedNode) {
                    if ($(addedNode).hasClass('card')) {
                        if ($(addedNode).parents('.top-ten').length > 0) {
                            var index = parseInt($(addedNode).attr('data-index'));
                            $(addedNode).attr('data-number', index + 1);
                        }
                    }
                });
            }
        });
    }
}

setTimeout(function () {
    HomeScreenSections.init();
}, 50);
